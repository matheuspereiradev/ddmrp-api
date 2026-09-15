using Microsoft.EntityFrameworkCore;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;
using Service.Infra.Data.Context;
using Service.Infra.Data.Helpers;

namespace Service.Infra.Data.Repositories
{
    public class BufferAdjustmentFactorRepository : BaseRepository<BufferAdjustmentFactor>, IBufferAdjustmentFactorRepository
    {
        public BufferAdjustmentFactorRepository(ApplicationDbContext context, ICurrentUserService currentUser) : base(context, currentUser)
        {
        }

        protected override IQueryable<BufferAdjustmentFactor> ApplyIncludes(IQueryable<BufferAdjustmentFactor> query) =>
            query.Include(b => b.Product).Include(b => b.Center);

        public async Task<bool> ExistsOverlappingAsync(int idProduct, int idCenter, DateTime effectiveFrom, DateTime effectiveTo, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(b =>
                b.deletedAt == null
                && b.IsActive
                && b.IdProduct == idProduct
                && b.IdCenter == idCenter
                && b.EffectiveFrom <= effectiveTo
                && b.EffectiveTo >= effectiveFrom,
                cancellationToken);
        }

        public async Task<PagedList<BufferAdjustmentFactor>> GetFilteredAsync(int? idProduct, int? idCenter, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = ApplyIncludes(_dbSet.AsQueryable()).Where(b => b.deletedAt == null);

            if (idProduct.HasValue)
                query = query.Where(b => b.IdProduct == idProduct.Value);

            if (idCenter.HasValue)
                query = query.Where(b => b.IdCenter == idCenter.Value);

            return await PaginationHelper.CreateAsync(query, pageNumber, pageSize, cancellationToken);
        }
    }
}
