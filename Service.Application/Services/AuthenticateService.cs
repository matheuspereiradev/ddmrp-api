using Service.Application.DTOs.User;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Service.Application.Services
{
    public class AuthenticateService : IAuthenticateService
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuthenticate _authenticate;

        public AuthenticateService(IUserRepository userRepository, IAuthenticate authenticate)
        {
            _userRepository = userRepository;
            _authenticate = authenticate;
        }
        public async Task<UserGetDto> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByEmail(email, cancellationToken);
            if (user == null)
                throw new BadRequestException("Invalid credentials");

            bool validPassword = BCrypt.Net.BCrypt.Verify(password, user.Password);
            if (!validPassword)
                throw new BadRequestException("Invalid credentials");


            return new UserGetDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email
            };
        }

        public string GenerateToken(int id, string email)
        {
            return _authenticate.GenerateToken(id, email);
        }
    }
}
