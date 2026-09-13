using Microsoft.EntityFrameworkCore;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Repositories
{
    public class CenterProductRepository : BaseRepository<CenterProduct>, ICenterProductRepository
    {
        public CenterProductRepository(ApplicationDbContext context, ICurrentUserService currentUser) : base(context, currentUser)
        {
        }

        protected override IQueryable<CenterProduct> ApplyIncludes(IQueryable<CenterProduct> query) =>
            query
                .Include(cp => cp.Product)
                .Include(cp => cp.Center)
                .Include(cp => cp.OriginCenter)
                .Include(cp => cp.Provider)
                .Include(cp => cp.Tag)
                .Include(cp => cp.Reason)
                .Include(cp => cp.AllocationGroup)
                .Include(cp => cp.BufferProfile);

        public async Task<CenterProduct> GetByProductAndCenterAsync(int idProduct, int idCenter, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FirstOrDefaultAsync(
                cp => cp.IdProduct == idProduct && cp.IdCenter == idCenter && cp.deletedAt == null,
                cancellationToken);
        }
    }
}
