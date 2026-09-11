using Service.Application.DTOs.User;
using Service.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Application.Interfaces
{
    public interface IAuthenticateService
    {
        Task<UserGetDto> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default);
        string GenerateToken(int id, string email);
    }
}
