using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;
using Service.Infra.Data.Context;
using Service.Infra.Data.Helpers;
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

        public async Task<PagedList<Product>> GetByCenterAsync(int idCenter, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _dbSet
                .Where(p => p.deletedAt == null
                    && _context.CenterProduct.Any(cp => cp.IdProduct == p.Id && cp.IdCenter == idCenter && cp.deletedAt == null));

            return await PaginationHelper.CreateAsync(query, pageNumber, pageSize, cancellationToken);
        }
    }
}
