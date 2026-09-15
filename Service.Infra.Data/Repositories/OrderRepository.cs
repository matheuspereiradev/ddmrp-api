using Microsoft.EntityFrameworkCore;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;
using Service.Infra.Data.Context;
using Service.Infra.Data.Helpers;

namespace Service.Infra.Data.Repositories
{
    public class OrderRepository : BaseRepository<Order>, IOrderRepository
    {
        public OrderRepository(ApplicationDbContext context, ICurrentUserService currentUser) : base(context, currentUser)
        {
        }

        protected override IQueryable<Order> ApplyIncludes(IQueryable<Order> query) =>
            query
                .Include(o => o.Product)
                .Include(o => o.Partner)
                .Include(o => o.DestinyCenter)
                .Include(o => o.OriginCenter);

        public async Task<PagedList<Order>> GetFilteredAsync(int? idDestinyCenter, int? idOriginCenter, int? idProduct, bool? fictional, bool? isInbound, bool? isOutbound, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = ApplyIncludes(_dbSet.AsQueryable()).Where(o => o.deletedAt == null);

            if (idDestinyCenter.HasValue)
                query = query.Where(o => o.IdDestinyCenter == idDestinyCenter.Value);

            if (idOriginCenter.HasValue)
                query = query.Where(o => o.IdOriginCenter == idOriginCenter.Value);

            if (idProduct.HasValue)
                query = query.Where(o => o.IdProduct == idProduct.Value);

            if (fictional.HasValue)
                query = query.Where(o => o.IsFictional == fictional.Value);

            if (isInbound.HasValue)
                query = query.Where(o => o.IsInbound == isInbound.Value);

            if (isOutbound.HasValue)
                query = query.Where(o => o.IsOutbound == isOutbound.Value);

            return await PaginationHelper.CreateAsync(query, pageNumber, pageSize, cancellationToken);
        }
    }
}
