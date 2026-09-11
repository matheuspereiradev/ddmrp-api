using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;
using Service.Infra.Data.Context;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Service.Infra.Data.Repositories
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context, ICurrentUserService currentUser) : base(context, currentUser)
        {
        }

        public async Task<User> GetByEmail(string email, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email && u.deletedAt == null, cancellationToken);
        }

    }

}
