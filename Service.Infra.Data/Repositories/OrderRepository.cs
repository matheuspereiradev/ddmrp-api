using Microsoft.EntityFrameworkCore;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

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
    }
}
