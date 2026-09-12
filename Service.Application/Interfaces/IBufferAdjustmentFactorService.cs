using Service.Application.DTOs.BufferAdjustmentFactor;
using Service.Domain.Entities;

namespace Service.Application.Interfaces
{
    public interface IBufferAdjustmentFactorService : IBaseService<BufferAdjustmentFactor, BufferAdjustmentFactorGetDto, BufferAdjustmentFactorPostDto, BufferAdjustmentFactorPutDto>
    {
        Task<BufferAdjustmentFactorGetDto> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);
    }
}
