using Service.Application.DTOs.DemandAdjustmentFactor;
using Service.Domain.Entities;

namespace Service.Application.Interfaces
{
    public interface IDemandAdjustmentFactorService : IBaseService<DemandAdjustmentFactor, DemandAdjustmentFactorGetDto, DemandAdjustmentFactorPostDto, DemandAdjustmentFactorPutDto>
    {
        Task<DemandAdjustmentFactorGetDto> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);
    }
}
