using Service.Application.DTOs.User;
using Service.Domain.Entities;
using Service.Domain.Pagination;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Application.Interfaces
{
    public interface IUserService : IBaseService<User, UserGetDto, UserPostDto, UserPutDto>
    {
        Task<UserGetDto> GetUserByEmail(string email, CancellationToken cancellationToken = default);
    }

}