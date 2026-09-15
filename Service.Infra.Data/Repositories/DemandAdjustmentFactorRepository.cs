using Microsoft.EntityFrameworkCore;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;
using Service.Infra.Data.Context;
using Service.Infra.Data.Helpers;

namespace Service.Infra.Data.Repositories
{
    public class DemandAdjustmentFactorRepository : BaseRepository<DemandAdjustmentFactor>, IDemandAdjustmentFactorRepository
    {
        public DemandAdjustmentFactorRepository(ApplicationDbContext context, ICurrentUserService currentUser) : base(context, currentUser)
        {
        }

        protected override IQueryable<DemandAdjustmentFactor> ApplyIncludes(IQueryable<DemandAdjustmentFactor> query) =>
            query.Include(d => d.Product).Include(d => d.Center);

        public async Task<bool> ExistsOverlappingAsync(int idProduct, int idCenter, DateTime effectiveFrom, DateTime effectiveTo, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(d =>
                d.deletedAt == null
                && d.IsActive
                && d.IdProduct == idProduct
                && d.IdCenter == idCenter
                && d.EffectiveFrom <= effectiveTo
                && d.EffectiveTo >= effectiveFrom,
                cancellationToken);
        }

        public async Task<PagedList<DemandAdjustmentFactor>> GetFilteredAsync(int? idProduct, int? idCenter, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = ApplyIncludes(_dbSet.AsQueryable()).Where(d => d.deletedAt == null);

            if (idProduct.HasValue)
                query = query.Where(d => d.IdProduct == idProduct.Value);

            if (idCenter.HasValue)
                query = query.Where(d => d.IdCenter == idCenter.Value);

            return await PaginationHelper.CreateAsync(query, pageNumber, pageSize, cancellationToken);
        }
    }
}
