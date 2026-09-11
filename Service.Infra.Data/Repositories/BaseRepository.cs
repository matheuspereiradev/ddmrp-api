using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;
using Service.Infra.Data.Context;
using Service.Infra.Data.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Service.Infra.Data.Repositories
{
    public class BaseRepository<T> : IBaseRepository<T> where T : BaseEntity
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;
        protected readonly ICurrentUserService _currentUser;

        public BaseRepository(ApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _dbSet = _context.Set<T>();
            _currentUser = currentUser;
        }

        public async Task<T> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FirstOrDefaultAsync(e => e.Id == id && e.deletedAt == null, cancellationToken);
        }

        public async Task<PagedList<T>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.Where(e => e.deletedAt == null).AsQueryable();
            return await PaginationHelper.CreateAsync(query, pageNumber, pageSize, cancellationToken);
        }

        public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            entity.createdAt = DateTime.UtcNow;
            entity.createdBy = _currentUser.UserId;
            await _dbSet.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            entity.updatedAt = DateTime.UtcNow;
            entity.updatedBy = _currentUser.UserId;
            _dbSet.Update(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task<T> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _dbSet.FirstOrDefaultAsync(e => e.Id == id && e.deletedAt == null, cancellationToken);
            if (entity == null)
                return null;

            entity.deletedAt = DateTime.UtcNow;
            entity.deletedBy = _currentUser.UserId;
            _dbSet.Update(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task<bool> Exists(int id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(e => e.Id == id && e.deletedAt == null, cancellationToken);
        }
    }
}