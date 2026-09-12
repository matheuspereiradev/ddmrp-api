using Service.Application.DTOs.AllocationGroup;
using Service.Application.Interfaces;
using Service.Application.Mappers;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Application.Services
{
    public class AllocationGroupService : BaseService<AllocationGroup, AllocationGroupGetDto, AllocationGroupPostDto, AllocationGroupPutDto>, IAllocationGroupService
    {
        public AllocationGroupService(IAllocationGroupRepository repository) : base(repository)
        {
        }

        protected override AllocationGroupGetDto ToGetDTO(AllocationGroup entity) => entity.ToGetDto();

        protected override AllocationGroup ToEntity(AllocationGroupPostDto postDTO)
        {
            return new AllocationGroup
            {
                Name = postDTO.Name
            };
        }

        protected override void ApplyUpdate(AllocationGroup entity, AllocationGroupPutDto putDTO)
        {
            entity.Name = putDTO.Name;
        }
    }
}
