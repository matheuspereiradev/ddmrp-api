using Service.Application.DTOs.DemandAdjustmentFactor;
using Service.Domain.Entities;
using Service.Domain.Pagination;

namespace Service.Application.Interfaces
{
    public interface IDemandAdjustmentFactorService : IBaseService<DemandAdjustmentFactor, DemandAdjustmentFactorGetDto, DemandAdjustmentFactorPostDto, DemandAdjustmentFactorPutDto>
    {
        Task<DemandAdjustmentFactorGetDto> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);
        Task<PagedList<DemandAdjustmentFactorGetDto>> GetFilteredAsync(int? idProduct, int? idCenter, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    }
}
