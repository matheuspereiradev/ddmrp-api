using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Repositories
{
    public class ImporterRepository : BaseRepository<Importer>, IImporterRepository
    {
        public ImporterRepository(ApplicationDbContext context, ICurrentUserService currentUser) : base(context, currentUser)
        {
        }
    }
}
