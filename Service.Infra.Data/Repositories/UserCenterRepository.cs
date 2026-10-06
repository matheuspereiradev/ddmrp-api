using Microsoft.EntityFrameworkCore;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Repositories
{
    public class UserCenterRepository : IUserCenterRepository
    {
        private readonly ApplicationDbContext _context;

        public UserCenterRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyCollection<int>?> GetAllowedCenterIdsAsync(int userId, CancellationToken cancellationToken = default)
        {
            if (await HasAllCentersAccessAsync(userId, cancellationToken))
                return null;

            return await _context.UserCenter
                .Where(uc => uc.IdUser == userId && uc.Center.deletedAt == null)
                .Select(uc => uc.IdCenter)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Center>> GetAssignedCentersAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _context.UserCenter
                .Where(uc => uc.IdUser == userId && uc.Center.deletedAt == null)
                .Select(uc => uc.Center)
                .OrderBy(c => c.Code)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<int>> GetAccessibleCenterIdsAsync(int userId, CancellationToken cancellationToken = default)
        {
            if (await HasAllCentersAccessAsync(userId, cancellationToken))
            {
                return await _context.Center
                    .Where(c => c.deletedAt == null)
                    .Select(c => c.Id)
                    .OrderBy(id => id)
                    .ToListAsync(cancellationToken);
            }

            return await _context.UserCenter
                .Where(uc => uc.IdUser == userId && uc.Center.deletedAt == null)
                .Select(uc => uc.IdCenter)
                .OrderBy(id => id)
                .ToListAsync(cancellationToken);
        }

        public async Task ReplaceAsync(int userId, IReadOnlyCollection<int> centerIds, CancellationToken cancellationToken = default)
        {
            var target = centerIds.ToHashSet();
            var existing = await _context.UserCenter
                .Where(uc => uc.IdUser == userId)
                .ToListAsync(cancellationToken);

            _context.UserCenter.RemoveRange(existing.Where(uc => !target.Contains(uc.IdCenter)));

            var present = existing.Select(uc => uc.IdCenter).ToHashSet();
            _context.UserCenter.AddRange(target
                .Where(idCenter => !present.Contains(idCenter))
                .Select(idCenter => new UserCenter { IdUser = userId, IdCenter = idCenter }));

            await _context.SaveChangesAsync(cancellationToken);
        }

        private Task<bool> HasAllCentersAccessAsync(int userId, CancellationToken cancellationToken)
        {
            return _context.User
                .Where(u => u.Id == userId && u.deletedAt == null)
                .Select(u => u.HasAllCentersAccess)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
