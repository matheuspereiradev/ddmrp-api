using Microsoft.EntityFrameworkCore;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Repositories
{
    public class ZoneAdjustmentFactorRepository : BaseRepository<ZoneAdjustmentFactor>, IZoneAdjustmentFactorRepository
    {
        public ZoneAdjustmentFactorRepository(ApplicationDbContext context, ICurrentUserService currentUser) : base(context, currentUser)
        {
        }

        protected override IQueryable<ZoneAdjustmentFactor> ApplyIncludes(IQueryable<ZoneAdjustmentFactor> query) =>
            query.Include(z => z.Product).Include(z => z.Center);
    }
}
