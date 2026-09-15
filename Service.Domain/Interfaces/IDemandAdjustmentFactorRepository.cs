using Service.Domain.Entities;
using Service.Domain.Pagination;

namespace Service.Domain.Interfaces
{
    public interface IDemandAdjustmentFactorRepository : IBaseRepository<DemandAdjustmentFactor>
    {
        Task<bool> ExistsOverlappingAsync(int idProduct, int idCenter, DateTime effectiveFrom, DateTime effectiveTo, CancellationToken cancellationToken = default);
        Task<PagedList<DemandAdjustmentFactor>> GetFilteredAsync(int? idProduct, int? idCenter, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    }
}
