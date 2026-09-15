using Service.Application.DTOs.ZoneAdjustmentFactor;
using Service.Domain.Entities;
using Service.Domain.Pagination;

namespace Service.Application.Interfaces
{
    public interface IZoneAdjustmentFactorService : IBaseService<ZoneAdjustmentFactor, ZoneAdjustmentFactorGetDto, ZoneAdjustmentFactorPostDto, ZoneAdjustmentFactorPutDto>
    {
        Task<ZoneAdjustmentFactorGetDto> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);
        Task<PagedList<ZoneAdjustmentFactorGetDto>> GetFilteredAsync(int? idProduct, int? idCenter, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    }
}
