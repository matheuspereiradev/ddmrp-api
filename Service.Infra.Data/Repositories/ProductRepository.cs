using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Service.Infra.Data.Repositories
{
    public class ProductRepository : BaseRepository<Product>, IProductRepository
    {
        public ProductRepository(ApplicationDbContext context, ICurrentUserService currentUser) : base(context, currentUser)
        {
        }

        public async Task<Dictionary<string, int>> GetIdsByReferencesAsync(IEnumerable<string> references, CancellationToken cancellationToken = default)
        {
            var referenceList = references.Distinct().ToList();
            return await _dbSet
                .Where(p => p.deletedAt == null && referenceList.Contains(p.Reference))
                .ToDictionaryAsync(p => p.Reference, p => p.Id, cancellationToken);
        }
    }
}
