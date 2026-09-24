using Microsoft.EntityFrameworkCore;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Repositories
{
    public class CalendarRepository : ICalendarRepository
    {
        private readonly ApplicationDbContext _context;

        public CalendarRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Matched by the real Date.DayOfWeek (computed in memory after fetching), not the stored
        // DayOfWeekNumber column — that column's numeric convention comes from a manually-run seed
        // script outside this codebase, so it can't be trusted to line up with .NET's DayOfWeek enum.
        public async Task UpdateFutureWorkingDaysAsync(DayOfWeek dayOfWeek, bool isWorkingDay, DateTime afterDate, CancellationToken cancellationToken = default)
        {
            var futureDays = await _context.Calendar
                .Where(c => c.Date > afterDate)
                .ToListAsync(cancellationToken);

            foreach (var day in futureDays.Where(d => d.Date.DayOfWeek == dayOfWeek))
                day.IsWorkingDay = isWorkingDay;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
