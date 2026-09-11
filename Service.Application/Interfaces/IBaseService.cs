using Service.Domain.Entities;
using Service.Domain.Pagination;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Application.Interfaces
{
    public interface IBaseService<TEntity, TGetDTO, TPostDTO, TPutDTO>
        where TEntity : BaseEntity
    {
        Task<TGetDTO> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<PagedList<TGetDTO>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<TGetDTO> AddAsync(TPostDTO postDTO, CancellationToken cancellationToken = default);
        Task<TGetDTO> UpdateAsync(int id, TPutDTO putDTO, CancellationToken cancellationToken = default);
        Task<TGetDTO> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
