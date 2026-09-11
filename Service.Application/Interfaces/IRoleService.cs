using Service.Application.DTOs.Role;
using Service.Domain.Entities;

namespace Service.Application.Interfaces
{
    public interface IRoleService : IBaseService<Role, RoleGetDto, RolePostDto, RolePutDto>
    {
    }
}
