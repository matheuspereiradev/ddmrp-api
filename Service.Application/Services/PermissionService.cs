using Service.Application.DTOs.Permission;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Application.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly IRoleRepository _roleRepository;

        public PermissionService(IPermissionRepository permissionRepository, IRoleRepository roleRepository)
        {
            _permissionRepository = permissionRepository;
            _roleRepository = roleRepository;
        }

        public async Task<List<PermissionGetDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var permissions = await _permissionRepository.GetAllAsync(cancellationToken);
            return permissions.Select(ToGetDto).ToList();
        }

        public async Task<List<PermissionGetDto>> GetByRoleAsync(int idRole, CancellationToken cancellationToken = default)
        {
            if (!await _roleRepository.Exists(idRole, cancellationToken))
                throw new NotFoundException("Role not found.");

            var permissions = await _permissionRepository.GetByRoleAsync(idRole, cancellationToken);
            return permissions.Select(ToGetDto).ToList();
        }

        public Task<HashSet<string>> GetPermissionKeysForRoleAsync(int idRole, CancellationToken cancellationToken = default)
        {
            return _permissionRepository.GetPermissionKeysForRoleAsync(idRole, cancellationToken);
        }

        public async Task ReplaceRolePermissionsAsync(int idRole, ReplaceRolePermissionsDto dto, CancellationToken cancellationToken = default)
        {
            if (!await _roleRepository.Exists(idRole, cancellationToken))
                throw new NotFoundException("Role not found.");

            var requestedIds = dto.PermissionIds.Distinct().ToList();
            var existingIds = await _permissionRepository.GetExistingIdsAsync(requestedIds, cancellationToken);
            var unknownIds = requestedIds.Except(existingIds).ToList();
            if (unknownIds.Count > 0)
                throw new BadRequestException($"Unknown permission id(s): {string.Join(", ", unknownIds)}");

            await _permissionRepository.ReplaceRolePermissionsAsync(idRole, requestedIds, cancellationToken);
        }

        public async Task GrantToRoleAsync(int idRole, string idPermission, CancellationToken cancellationToken = default)
        {
            if (!await _roleRepository.Exists(idRole, cancellationToken))
                throw new NotFoundException("Role not found.");

            var existingIds = await _permissionRepository.GetExistingIdsAsync([idPermission], cancellationToken);
            if (existingIds.Count == 0)
                throw new BadRequestException($"Unknown permission id: {idPermission}");

            await _permissionRepository.AddRolePermissionAsync(idRole, idPermission, cancellationToken);
        }

        public async Task RevokeFromRoleAsync(int idRole, string idPermission, CancellationToken cancellationToken = default)
        {
            if (!await _roleRepository.Exists(idRole, cancellationToken))
                throw new NotFoundException("Role not found.");

            await _permissionRepository.RemoveRolePermissionAsync(idRole, idPermission, cancellationToken);
        }

        private static PermissionGetDto ToGetDto(Permission entity) => new()
        {
            Id = entity.Id,
            Description = entity.Description,
            Module = entity.Module
        };
    }
}
