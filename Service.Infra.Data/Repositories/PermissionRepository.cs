using Microsoft.EntityFrameworkCore;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Repositories
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly ApplicationDbContext _context;

        public PermissionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Permission>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Permission
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<string>> GetExistingIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
        {
            var idList = ids.Distinct().ToList();
            return await _context.Permission
                .Where(p => idList.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Permission>> GetByRoleAsync(int idRole, CancellationToken cancellationToken = default)
        {
            return await _context.RolePermission
                .Where(rp => rp.IdRole == idRole)
                .Select(rp => rp.Permission)
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<HashSet<string>> GetPermissionKeysForRoleAsync(int idRole, CancellationToken cancellationToken = default)
        {
            var keys = await _context.RolePermission
                .Where(rp => rp.IdRole == idRole)
                .Select(rp => rp.IdPermission)
                .ToListAsync(cancellationToken);

            return keys.ToHashSet();
        }

        public async Task ReplaceRolePermissionsAsync(int idRole, List<string> permissionIds, CancellationToken cancellationToken = default)
        {
            var existing = await _context.RolePermission
                .Where(rp => rp.IdRole == idRole)
                .ToListAsync(cancellationToken);

            var toRemove = existing.Where(rp => !permissionIds.Contains(rp.IdPermission)).ToList();
            var existingIds = existing.Select(rp => rp.IdPermission).ToHashSet();
            var toAdd = permissionIds
                .Where(id => !existingIds.Contains(id))
                .Select(id => new RolePermission { IdRole = idRole, IdPermission = id })
                .ToList();

            _context.RolePermission.RemoveRange(toRemove);
            await _context.RolePermission.AddRangeAsync(toAdd, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task AddRolePermissionAsync(int idRole, string idPermission, CancellationToken cancellationToken = default)
        {
            var exists = await _context.RolePermission
                .AnyAsync(rp => rp.IdRole == idRole && rp.IdPermission == idPermission, cancellationToken);
            if (exists)
                return;

            await _context.RolePermission.AddAsync(new RolePermission { IdRole = idRole, IdPermission = idPermission }, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task RemoveRolePermissionAsync(int idRole, string idPermission, CancellationToken cancellationToken = default)
        {
            var existing = await _context.RolePermission
                .FirstOrDefaultAsync(rp => rp.IdRole == idRole && rp.IdPermission == idPermission, cancellationToken);
            if (existing == null)
                return;

            _context.RolePermission.Remove(existing);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
