using Microsoft.EntityFrameworkCore;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;
using Service.Infra.Data.Context;
using Service.Infra.Data.Helpers;

namespace Service.Infra.Data.Repositories
{
    public class NoteRepository : BaseRepository<Note>, INoteRepository
    {
        public NoteRepository(ApplicationDbContext context, ICurrentUserService currentUser) : base(context, currentUser)
        {
        }

        protected override IQueryable<Note> ApplyIncludes(IQueryable<Note> query) =>
            query.Include(n => n.CreatedByUser);

        public async Task<PagedList<Note>> GetByCenterProductAsync(int idCenterProduct, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = ApplyIncludes(_dbSet.AsQueryable())
                .Where(n => n.deletedAt == null && n.CenterProductId == idCenterProduct);

            return await PaginationHelper.CreateAsync(query, pageNumber, pageSize, cancellationToken);
        }
    }
}
