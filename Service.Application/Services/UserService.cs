using Service.Application.DTOs.User;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Application.Mappers;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Application.Services
{
    public class UserService : BaseService<User, UserGetDto, UserPostDto, UserPutDto>, IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;

        public UserService(IUserRepository userRepository, IRoleRepository roleRepository)
            : base(userRepository)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
        }

        protected override UserGetDto ToGetDTO(User entity)
        {
            return new UserGetDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Email = entity.Email,
                IdRole = entity.IdRole,
                Role = entity.Role?.ToGetDto()
            };
        }

        protected override User ToEntity(UserPostDto postDTO)
        {
            return new User
            {
                Name = postDTO.Name,
                Email = postDTO.Email,
                IdRole = postDTO.IdRole
            };
        }

        protected override void ApplyUpdate(User entity, UserPutDto putDTO)
        {
            entity.Name = putDTO.Name;
            entity.Email = putDTO.Email;
            entity.IdRole = putDTO.IdRole;
        }

        public override async Task<UserGetDto> AddAsync(UserPostDto postDTO, CancellationToken cancellationToken = default)
        {
            var existingUser = await _userRepository.GetByEmail(postDTO.Email, cancellationToken);
            if (existingUser != null)
                throw new BadRequestException("Email already in use.");

            if (!await _roleRepository.Exists(postDTO.IdRole, cancellationToken))
                throw new BadRequestException("Role not found.");

            var user = ToEntity(postDTO);
            user.Password = BCrypt.Net.BCrypt.HashPassword(postDTO.Password);

            var created = await _userRepository.AddAsync(user, cancellationToken);
            return ToGetDTO(created);
        }

        public override async Task<UserGetDto> UpdateAsync(int id, UserPutDto putDTO, CancellationToken cancellationToken = default)
        {
            if (!await _roleRepository.Exists(putDTO.IdRole, cancellationToken))
                throw new BadRequestException("Role not found.");

            return await base.UpdateAsync(id, putDTO, cancellationToken);
        }

        public async Task<UserGetDto> GetUserByEmail(string email, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByEmail(email, cancellationToken);
            if (user == null)
                throw new NotFoundException("User not found");
            return ToGetDTO(user);
        }
    }

}
