using Microsoft.EntityFrameworkCore;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Enums;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;
using Service.Infra.Data.Context;
using Service.Infra.Data.Helpers;

namespace Service.Infra.Data.Repositories
{
    public class ZoneAdjustmentFactorRepository : BaseRepository<ZoneAdjustmentFactor>, IZoneAdjustmentFactorRepository
    {
        public ZoneAdjustmentFactorRepository(ApplicationDbContext context, ICurrentUserService currentUser) : base(context, currentUser)
        {
        }

        protected override IQueryable<ZoneAdjustmentFactor> ApplyIncludes(IQueryable<ZoneAdjustmentFactor> query) =>
            query.Include(z => z.Product).Include(z => z.Center);

        public async Task<bool> ExistsOverlappingAsync(int idProduct, int idCenter, TargetZone targetZone, DateTime effectiveFrom, DateTime effectiveTo, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(z =>
                z.deletedAt == null
                && z.IsActive
                && z.IdProduct == idProduct
                && z.IdCenter == idCenter
                && z.TargetZone == targetZone
                && z.EffectiveFrom <= effectiveTo
                && z.EffectiveTo >= effectiveFrom,
                cancellationToken);
        }

        public async Task<PagedList<ZoneAdjustmentFactor>> GetFilteredAsync(int? idProduct, int? idCenter, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = ApplyIncludes(_dbSet.AsQueryable()).Where(z => z.deletedAt == null);

            if (idProduct.HasValue)
                query = query.Where(z => z.IdProduct == idProduct.Value);

            if (idCenter.HasValue)
                query = query.Where(z => z.IdCenter == idCenter.Value);

            return await PaginationHelper.CreateAsync(query, pageNumber, pageSize, cancellationToken);
        }
    }
}
