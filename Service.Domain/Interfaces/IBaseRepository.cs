using Service.Domain.Entities;
using Service.Domain.Pagination;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Domain.Interfaces
{
    public interface IBaseRepository<T> where T : BaseEntity
    {
        Task<T> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<PagedList<T>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
        Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);
        Task<T> DeleteAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> Exists(int id, CancellationToken cancellationToken = default);
    }
}
