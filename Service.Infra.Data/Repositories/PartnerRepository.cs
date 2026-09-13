using Microsoft.EntityFrameworkCore;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Repositories
{
    public class PartnerRepository : BaseRepository<Partner>, IPartnerRepository
    {
        public PartnerRepository(ApplicationDbContext context, ICurrentUserService currentUser) : base(context, currentUser)
        {
        }

        public async Task<Dictionary<string, int>> GetIdsByCodesAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default)
        {
            var codeList = codes.Distinct().ToList();
            return await _dbSet
                .Where(p => p.deletedAt == null && codeList.Contains(p.Code))
                .ToDictionaryAsync(p => p.Code, p => p.Id, cancellationToken);
        }
    }
}
