using Service.Application.DTOs.BufferAdjustmentFactor;
using Service.Domain.Entities;
using Service.Domain.Pagination;

namespace Service.Application.Interfaces
{
    public interface IBufferAdjustmentFactorService : IBaseService<BufferAdjustmentFactor, BufferAdjustmentFactorGetDto, BufferAdjustmentFactorPostDto, BufferAdjustmentFactorPutDto>
    {
        Task<BufferAdjustmentFactorGetDto> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);
        Task<PagedList<BufferAdjustmentFactorGetDto>> GetFilteredAsync(int? idProduct, int? idCenter, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    }
}
