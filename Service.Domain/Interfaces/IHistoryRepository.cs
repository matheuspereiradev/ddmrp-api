using Service.Domain.Entities;
using Service.Domain.Pagination;

namespace Service.Domain.Interfaces
{
    public interface IHistoryRepository : IBaseRepository<History>
    {
        Task<PagedList<History>> GetFilteredAsync(int? idProduct, int? idCenter, DateTime? dateStart, DateTime? dateEnd, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    }
}
