using Microsoft.EntityFrameworkCore;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;
using Service.Infra.Data.Context;
using Service.Infra.Data.Helpers;

namespace Service.Infra.Data.Repositories
{
    public class HistoryRepository : BaseRepository<History>, IHistoryRepository
    {
        public HistoryRepository(ApplicationDbContext context, ICurrentUserService currentUser) : base(context, currentUser)
        {
        }

        protected override IQueryable<History> ApplyIncludes(IQueryable<History> query) =>
            query.Include(h => h.Product).Include(h => h.Center).Include(h => h.BufferProfile);

        public async Task<PagedList<History>> GetFilteredAsync(int? idProduct, int? idCenter, DateTime? dateStart, DateTime? dateEnd, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = ApplyIncludes(_dbSet.AsQueryable()).Where(h => h.deletedAt == null);

            if (idProduct.HasValue)
                query = query.Where(h => h.IdProduct == idProduct.Value);

            if (idCenter.HasValue)
                query = query.Where(h => h.IdCenter == idCenter.Value);

            if (dateStart.HasValue)
                query = query.Where(h => h.Date >= dateStart.Value);

            if (dateEnd.HasValue)
                query = query.Where(h => h.Date <= dateEnd.Value);

            return await PaginationHelper.CreateAsync(query, pageNumber, pageSize, cancellationToken);
        }
    }
}
