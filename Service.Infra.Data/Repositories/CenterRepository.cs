using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Service.Infra.Data.Repositories
{
    public class CenterRepository : BaseRepository<Center>, ICenterRepository
    {
        public CenterRepository(ApplicationDbContext context, ICurrentUserService currentUser) : base(context, currentUser)
        {
        }

        public async Task<Dictionary<string, int>> GetIdsByCodesAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default)
        {
            var codeList = codes.Distinct().ToList();
            return await _dbSet
                .Where(c => c.deletedAt == null && codeList.Contains(c.Code))
                .ToDictionaryAsync(c => c.Code, c => c.Id, cancellationToken);
        }

        public async Task<HashSet<int>> GetExistingIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
        {
            var idList = ids.Distinct().ToList();
            var existing = await _dbSet
                .Where(c => c.deletedAt == null && idList.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);
            return existing.ToHashSet();
        }
    }
}
