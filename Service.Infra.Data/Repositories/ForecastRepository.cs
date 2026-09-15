using Microsoft.EntityFrameworkCore;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;
using Service.Infra.Data.Context;
using Service.Infra.Data.Helpers;

namespace Service.Infra.Data.Repositories
{
    public class ForecastRepository : BaseRepository<Forecast>, IForecastRepository
    {
        public ForecastRepository(ApplicationDbContext context, ICurrentUserService currentUser) : base(context, currentUser)
        {
        }

        protected override IQueryable<Forecast> ApplyIncludes(IQueryable<Forecast> query) =>
            query.Include(f => f.Product).Include(f => f.Center);

        public async Task<PagedList<Forecast>> GetFilteredAsync(int? idProduct, int? idCenter, DateTime? dateStart, DateTime? dateEnd, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = ApplyIncludes(_dbSet.AsQueryable()).Where(f => f.deletedAt == null);

            if (idProduct.HasValue)
                query = query.Where(f => f.IdProduct == idProduct.Value);

            if (idCenter.HasValue)
                query = query.Where(f => f.IdCenter == idCenter.Value);

            if (dateStart.HasValue)
                query = query.Where(f => f.Date >= dateStart.Value);

            if (dateEnd.HasValue)
                query = query.Where(f => f.Date <= dateEnd.Value);

            return await PaginationHelper.CreateAsync(query, pageNumber, pageSize, cancellationToken);
        }
    }
}
