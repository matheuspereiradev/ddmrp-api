using Service.Application.DTOs.Auth;

namespace Service.Application.Interfaces
{
    public interface IAuthenticateService
    {
        Task<AuthResponseDto> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
        Task RevokeTokenAsync(int userId, string refreshToken, CancellationToken cancellationToken = default);
    }
}
