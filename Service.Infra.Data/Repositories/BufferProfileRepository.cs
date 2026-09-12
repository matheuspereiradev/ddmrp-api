using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Repositories
{
    public class BufferProfileRepository : BaseRepository<BufferProfile>, IBufferProfileRepository
    {
        public BufferProfileRepository(ApplicationDbContext context, ICurrentUserService currentUser) : base(context, currentUser)
        {
        }
    }
}
