using Service.Domain.Entities;
using Service.Domain.Pagination;

namespace Service.Domain.Interfaces
{
    public interface IOrderRepository : IBaseRepository<Order>
    {
        Task<PagedList<Order>> GetFilteredAsync(int? idDestinyCenter, int? idOriginCenter, bool? fictional, bool? isInbound, bool? isOutbound, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    }
}
