using Microsoft.EntityFrameworkCore;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Repositories
{
    public class BufferAdjustmentFactorRepository : BaseRepository<BufferAdjustmentFactor>, IBufferAdjustmentFactorRepository
    {
        public BufferAdjustmentFactorRepository(ApplicationDbContext context, ICurrentUserService currentUser) : base(context, currentUser)
        {
        }

        protected override IQueryable<BufferAdjustmentFactor> ApplyIncludes(IQueryable<BufferAdjustmentFactor> query) =>
            query.Include(b => b.Product).Include(b => b.Center);
    }
}
