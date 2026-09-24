using Service.Domain.Entities;

namespace Service.Domain.Interfaces
{
    public interface IPermissionRepository
    {
        Task<List<Permission>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<List<string>> GetExistingIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
        Task<List<Permission>> GetByRoleAsync(int idRole, CancellationToken cancellationToken = default);
        Task<HashSet<string>> GetPermissionKeysForRoleAsync(int idRole, CancellationToken cancellationToken = default);
        Task ReplaceRolePermissionsAsync(int idRole, List<string> permissionIds, CancellationToken cancellationToken = default);
        Task AddRolePermissionAsync(int idRole, string idPermission, CancellationToken cancellationToken = default);
        Task RemoveRolePermissionAsync(int idRole, string idPermission, CancellationToken cancellationToken = default);
    }
}
