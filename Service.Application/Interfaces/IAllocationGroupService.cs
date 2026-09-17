using Service.Application.DTOs.AllocationGroup;
using Service.Domain.AllocationGroups;
using Service.Domain.Entities;

namespace Service.Application.Interfaces
{
    public interface IAllocationGroupService : IBaseService<AllocationGroup, AllocationGroupGetDto, AllocationGroupPostDto, AllocationGroupPutDto>
    {
        Task<List<PriorizedAllocationRow>> GetPriorizedAllocationAsync(CancellationToken cancellationToken = default);
        Task<List<PriorizedAllocationItem>> RunPriorizedAllocationAsync(PriorizedAllocationRunDto runDto, CancellationToken cancellationToken = default);
    }
}
