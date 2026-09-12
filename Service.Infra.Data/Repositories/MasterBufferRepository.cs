using Microsoft.EntityFrameworkCore;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Repositories
{
    public class MasterBufferRepository : BaseRepository<MasterBuffer>, IMasterBufferRepository
    {
        public MasterBufferRepository(ApplicationDbContext context, ICurrentUserService currentUser) : base(context, currentUser)
        {
        }

        protected override IQueryable<MasterBuffer> ApplyIncludes(IQueryable<MasterBuffer> query) =>
            query
                .Include(mb => mb.Product)
                .Include(mb => mb.Center)
                .Include(mb => mb.ProductFather)
                .Include(mb => mb.CenterFather);
    }
}
