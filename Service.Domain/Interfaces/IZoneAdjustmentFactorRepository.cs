using Service.Domain.Entities;
using Service.Domain.Enums;
using Service.Domain.Pagination;

namespace Service.Domain.Interfaces
{
    public interface IZoneAdjustmentFactorRepository : IBaseRepository<ZoneAdjustmentFactor>
    {
        Task<bool> ExistsOverlappingAsync(int idProduct, int idCenter, TargetZone targetZone, DateTime effectiveFrom, DateTime effectiveTo, CancellationToken cancellationToken = default);
        Task<PagedList<ZoneAdjustmentFactor>> GetFilteredAsync(int? idProduct, int? idCenter, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    }
}
