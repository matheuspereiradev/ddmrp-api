using Service.Application.DTOs.Auth;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using System;

namespace Service.Application.Services
{
    public class AuthenticateService : IAuthenticateService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IAuthenticate _authenticate;

        public AuthenticateService(IUserRepository userRepository, IRefreshTokenRepository refreshTokenRepository, IAuthenticate authenticate)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _authenticate = authenticate;
        }

        public async Task<AuthResponseDto> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByEmail(email, cancellationToken);
            if (user == null)
                throw new BadRequestException("Invalid credentials");

            bool validPassword = BCrypt.Net.BCrypt.Verify(password, user.Password);
            if (!validPassword)
                throw new BadRequestException("Invalid credentials");

            return await IssueTokensAsync(user, cancellationToken);
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            var existing = await _refreshTokenRepository.GetByTokenAsync(refreshToken, cancellationToken);
            if (existing == null || !existing.IsActive)
                throw new UnauthorizedAccessException("Invalid refresh token.");

            var user = existing.User ?? await _userRepository.GetByIdAsync(existing.UserId, cancellationToken);
            if (user == null)
                throw new UnauthorizedAccessException("Invalid refresh token.");

            var response = await IssueTokensAsync(user, cancellationToken);

            existing.RevokedAt = DateTime.UtcNow;
            existing.ReplacedByToken = response.RefreshToken;
            await _refreshTokenRepository.UpdateAsync(existing, cancellationToken);

            return response;
        }

        public async Task RevokeTokenAsync(int userId, string refreshToken, CancellationToken cancellationToken = default)
        {
            var existing = await _refreshTokenRepository.GetByTokenAsync(refreshToken, cancellationToken);
            if (existing == null || existing.UserId != userId || !existing.IsActive)
                throw new BadRequestException("Invalid refresh token.");

            existing.RevokedAt = DateTime.UtcNow;
            await _refreshTokenRepository.UpdateAsync(existing, cancellationToken);
        }

        private async Task<AuthResponseDto> IssueTokensAsync(User user, CancellationToken cancellationToken)
        {
            var accessToken = _authenticate.GenerateToken(user.Id, user.Email, user.IdRole);
            var accessTokenExpiresAt = _authenticate.GetTokenExpiration();
            var refreshTokenValue = _authenticate.GenerateRefreshToken();
            var refreshTokenExpiresAt = _authenticate.GetRefreshTokenExpiration();

            var refreshToken = new RefreshToken
            {
                Token = refreshTokenValue,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = refreshTokenExpiresAt
            };
            await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

            return new AuthResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                AccessToken = accessToken,
                AccessTokenExpiresAt = accessTokenExpiresAt,
                RefreshToken = refreshTokenValue,
                RefreshTokenExpiresAt = refreshTokenExpiresAt
            };
        }
    }
}
