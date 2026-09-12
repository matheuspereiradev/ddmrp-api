using Service.Domain.Account;
using Microsoft.Extensions.Configuration;
using System;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace Service.Infra.Data.Identity
{
    public class AuthenticateProvider : IAuthenticate
    {
        private readonly IConfiguration _configuration;
        public AuthenticateProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(int id, string email)
        {
            var claims = new[]
            {
                new Claim("id", id.ToString()),
                new Claim("email", email.ToLower()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var privateKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
            var credentials = new SigningCredentials(privateKey, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: GetTokenExpiration(),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public DateTime GetTokenExpiration()
        {
            var minutes = int.TryParse(_configuration["Jwt:AccessTokenExpirationMinutes"], out var configured) ? configured : 60;
            return DateTime.UtcNow.AddMinutes(minutes);
        }

        public string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        public DateTime GetRefreshTokenExpiration()
        {
            var days = int.TryParse(_configuration["Jwt:RefreshTokenExpirationDays"], out var configured) ? configured : 7;
            return DateTime.UtcNow.AddDays(days);
        }
    }
}
