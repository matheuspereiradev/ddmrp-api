using Service.Domain.AllocationGroups;
using Service.Domain.Entities;

namespace Service.Domain.Interfaces
{
    public interface IAllocationGroupRepository : IBaseRepository<AllocationGroup>
    {
        Task<List<PriorizedAllocationRow>> GetPriorizedAllocationAsync(int idUser, CancellationToken cancellationToken = default);
    }
}
