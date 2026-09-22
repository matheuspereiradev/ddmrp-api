using Microsoft.EntityFrameworkCore;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Forecasts;
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

        // "Opens" each monthly/interval Forecast into one row per calendar day in its own [StartDate, EndDate]
        // window (Service.Domain.Entities.Calendar supplies the day spine — see CLAUDE.md's Forecast bullet).
        // Business days (Calendar.IsWorkingDay, set directly per date — Setting is no longer read here) get
        // an even split of Value; non-business days are still listed, with Value = 0 (not omitted) — same
        // convention the Adu "Futuro" calculation already uses.
        public async Task<PagedList<ForecastDailyRow>> GetFilteredAsync(int? idProduct, int? idCenter, DateTime? dateStart, DateTime? dateEnd, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var forecasts = _context.Forecast.Where(f => f.deletedAt == null);

            if (idProduct.HasValue)
                forecasts = forecasts.Where(f => f.IdProduct == idProduct.Value);

            if (idCenter.HasValue)
                forecasts = forecasts.Where(f => f.IdCenter == idCenter.Value);

            var workingDays = _context.Calendar.Where(c => c.IsWorkingDay);

            var withBusinessDayCount = forecasts.Select(f => new
            {
                Forecast = f,
                BusinessDayCount = workingDays.Count(c => c.Date >= f.StartDate && c.Date <= f.EndDate)
            });

            var exploded =
                from f in withBusinessDayCount
                from d in _context.Calendar
                where d.Date >= f.Forecast.StartDate && d.Date <= f.Forecast.EndDate
                select new ForecastDailyRow
                {
                    Id = f.Forecast.Id,
                    IdProduct = f.Forecast.IdProduct,
                    IdCenter = f.Forecast.IdCenter,
                    Date = d.Date,
                    Value = d.IsWorkingDay && f.BusinessDayCount > 0
                        ? f.Forecast.Value / f.BusinessDayCount
                        : 0,
                    Product = f.Forecast.Product,
                    Center = f.Forecast.Center
                };

            if (dateStart.HasValue)
                exploded = exploded.Where(r => r.Date >= dateStart.Value);

            if (dateEnd.HasValue)
                exploded = exploded.Where(r => r.Date <= dateEnd.Value);

            exploded = exploded.OrderBy(r => r.Date).ThenBy(r => r.Id);

            return await PaginationHelper.CreateAsync(exploded, pageNumber, pageSize, cancellationToken);
        }

        // The raw, un-exploded interval view — one row per stored Forecast record (Value + StartDate/EndDate),
        // for reports/consumers that want the monthly figure instead of the daily breakdown.
        public async Task<PagedList<Forecast>> GetGroupedAsync(int? idProduct, int? idCenter, DateTime? dateStart, DateTime? dateEnd, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = ApplyIncludes(_dbSet.AsQueryable()).Where(f => f.deletedAt == null);

            if (idProduct.HasValue)
                query = query.Where(f => f.IdProduct == idProduct.Value);

            if (idCenter.HasValue)
                query = query.Where(f => f.IdCenter == idCenter.Value);

            if (dateStart.HasValue)
                query = query.Where(f => f.EndDate >= dateStart.Value);

            if (dateEnd.HasValue)
                query = query.Where(f => f.StartDate <= dateEnd.Value);

            return await PaginationHelper.CreateAsync(query, pageNumber, pageSize, cancellationToken);
        }

        public async Task<Forecast> GetByProductCenterAndPeriodAsync(int idProduct, int idCenter, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            return await ApplyIncludes(_dbSet.AsQueryable())
                .FirstOrDefaultAsync(f => f.deletedAt == null
                    && f.IdProduct == idProduct
                    && f.IdCenter == idCenter
                    && f.StartDate == startDate
                    && f.EndDate == endDate, cancellationToken);
        }
    }
}
