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
                .Include(cp => cp.AllocationGroup);
    }
}
