using Service.Application.DTOs.AllocationGroup;
using Service.Domain.AllocationGroups;
using Service.Domain.Entities;

namespace Service.Application.Interfaces
{
    public interface IAllocationGroupService : IBaseService<AllocationGroup, AllocationGroupGetDto, AllocationGroupPostDto, AllocationGroupPutDto>
    {
        Task<List<EfficientDistributionRow>> GetEfficientDistributionAsync(CancellationToken cancellationToken = default);
        Task<List<EfficientDistributionAllocationItem>> RunEfficientDistributionAsync(EfficientDistributionRunDto runDto, CancellationToken cancellationToken = default);
    }
}
