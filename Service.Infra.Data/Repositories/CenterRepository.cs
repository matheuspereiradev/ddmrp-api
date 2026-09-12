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
    }
}
