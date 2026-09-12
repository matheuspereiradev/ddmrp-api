using Service.Application.DTOs.AllocationGroup;
using Service.Domain.Entities;

namespace Service.Application.Interfaces
{
    public interface IAllocationGroupService : IBaseService<AllocationGroup, AllocationGroupGetDto, AllocationGroupPostDto, AllocationGroupPutDto>
    {
    }
}
