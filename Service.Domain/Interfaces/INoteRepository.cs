using Service.Domain.Entities;
using Service.Domain.Pagination;

namespace Service.Domain.Interfaces
{
    public interface INoteRepository : IBaseRepository<Note>
    {
        Task<PagedList<Note>> GetByCenterProductAsync(int idCenterProduct, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    }
}
