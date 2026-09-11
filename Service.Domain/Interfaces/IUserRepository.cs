using Service.Domain.Entities;
using Service.Domain.Pagination;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Domain.Interfaces
{
    public interface IUserRepository : IBaseRepository<User>
    {
        Task<User> GetByEmail(string email, CancellationToken cancellationToken = default);
    }
}
