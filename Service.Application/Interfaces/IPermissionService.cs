using Service.Application.DTOs.Permission;

namespace Service.Application.Interfaces
{
    public interface IPermissionService
    {
        Task<List<PermissionGetDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<List<PermissionGetDto>> GetByRoleAsync(int idRole, CancellationToken cancellationToken = default);
        Task<HashSet<string>> GetPermissionKeysForRoleAsync(int idRole, CancellationToken cancellationToken = default);
        Task ReplaceRolePermissionsAsync(int idRole, ReplaceRolePermissionsDto dto, CancellationToken cancellationToken = default);
        Task GrantToRoleAsync(int idRole, string idPermission, CancellationToken cancellationToken = default);
        Task RevokeFromRoleAsync(int idRole, string idPermission, CancellationToken cancellationToken = default);
    }
}
