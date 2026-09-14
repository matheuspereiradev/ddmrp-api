using Microsoft.EntityFrameworkCore;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

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
    }
}
