using Service.Domain.Entities;

namespace Service.Domain.Interfaces
{
    public interface IBufferAdjustmentFactorRepository : IBaseRepository<BufferAdjustmentFactor>
    {
        Task<bool> ExistsOverlappingAsync(int idProduct, int idCenter, DateTime effectiveFrom, DateTime effectiveTo, CancellationToken cancellationToken = default);
    }
}
