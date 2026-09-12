using System;

namespace Service.Domain.Account
{
    public interface IAuthenticate
    {
        string GenerateToken(int id, string email);
        DateTime GetTokenExpiration();
        string GenerateRefreshToken();
        DateTime GetRefreshTokenExpiration();
    }
}
