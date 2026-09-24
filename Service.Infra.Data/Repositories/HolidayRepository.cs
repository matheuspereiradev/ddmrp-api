using Microsoft.EntityFrameworkCore;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;
using Service.Infra.Data.Context;
using Service.Infra.Data.Helpers;

namespace Service.Infra.Data.Repositories
{
    public class HolidayRepository : BaseRepository<Holiday>, IHolidayRepository
    {
        private readonly ICurrentUserService _currentUserService;

        public HolidayRepository(ApplicationDbContext context, ICurrentUserService currentUser) : base(context, currentUser)
        {
            _currentUserService = currentUser;
        }

        public async Task<PagedList<Holiday>> GetFilteredAsync(int pageNumber, int pageSize, bool includePast, CancellationToken cancellationToken = default)
        {
            var today = DateTime.UtcNow.Date;
            var query = _dbSet.Where(h => h.deletedAt == null);

            if (!includePast)
                query = query.Where(h => h.Date >= today);

            query = query.OrderBy(h => h.Date);

            return await PaginationHelper.CreateAsync(query, pageNumber, pageSize, cancellationToken);
        }

        public async Task<List<Holiday>> AddRangeAsync(List<Holiday> holidays, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var userId = _currentUserService.UserId;

            foreach (var holiday in holidays)
            {
                holiday.createdAt = now;
                holiday.createdBy = userId;
            }

            await _dbSet.AddRangeAsync(holidays, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return holidays;
        }
    }
}
