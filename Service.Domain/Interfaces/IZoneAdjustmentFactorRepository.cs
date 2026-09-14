using Service.Domain.Entities;
using Service.Domain.Enums;

namespace Service.Domain.Interfaces
{
    public interface IZoneAdjustmentFactorRepository : IBaseRepository<ZoneAdjustmentFactor>
    {
        Task<bool> ExistsOverlappingAsync(int idProduct, int idCenter, TargetZone targetZone, DateTime effectiveFrom, DateTime effectiveTo, CancellationToken cancellationToken = default);
    }
}
