using Service.Application.DTOs.Role;
using Service.Application.Interfaces;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Application.Services
{
    public class RoleService : BaseService<Role, RoleGetDto, RolePostDto, RolePutDto>, IRoleService
    {
        public RoleService(IRoleRepository repository) : base(repository)
        {
        }

        protected override RoleGetDto ToGetDTO(Role entity)
        {
            return new RoleGetDto
            {
                Id = entity.Id,
                Name = entity.Name
            };
        }

        protected override Role ToEntity(RolePostDto postDTO)
        {
            return new Role
            {
                Name = postDTO.Name
            };
        }

        protected override void ApplyUpdate(Role entity, RolePutDto putDTO)
        {
            entity.Name = putDTO.Name;
        }
    }
}
