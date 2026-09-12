using Service.Application.DTOs.ZoneAdjustmentFactor;
using Service.Domain.Entities;

namespace Service.Application.Interfaces
{
    public interface IZoneAdjustmentFactorService : IBaseService<ZoneAdjustmentFactor, ZoneAdjustmentFactorGetDto, ZoneAdjustmentFactorPostDto, ZoneAdjustmentFactorPutDto>
    {
        Task<ZoneAdjustmentFactorGetDto> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);
    }
}
