using Microsoft.EntityFrameworkCore;
using Service.Domain.Entities;
using Service.Infra.Data.Context;
using Service.Infra.Data.Repositories;

namespace Service.Tests.InfraData;

public class CalendarRepositoryTests
{
    private static CalendarRepository CreateSut(out ApplicationDbContext context)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        context = new ApplicationDbContext(options);
        return new CalendarRepository(context);
    }

    // DayOfWeekNumber is deliberately wrong/inconsistent (always 99) in every row below — this is the
    // regression test for the bug where matching relied on that stored column instead of the real
    // Date.DayOfWeek: a manually-run seed script outside this codebase decides that column's numeric
    // convention, which can't be trusted to match .NET's DayOfWeek enum, so the repository must never
    // filter by it.
    [Fact]
    public async Task UpdateFutureWorkingDaysAsync_MatchesByRealDate_IgnoringDayOfWeekNumberColumn()
    {
        var repository = CreateSut(out var context);
        var today = new DateTime(2026, 9, 24); // Thursday

        context.Calendar.AddRange(
            new Calendar { Date = today.AddDays(1), DayOfWeekNumber = 99, IsWorkingDay = true },  // Friday
            new Calendar { Date = today.AddDays(8), DayOfWeekNumber = 99, IsWorkingDay = true },  // Friday
            new Calendar { Date = today, DayOfWeekNumber = 99, IsWorkingDay = true },              // Thursday, today (not > today)
            new Calendar { Date = today.AddDays(-6), DayOfWeekNumber = 99, IsWorkingDay = true },  // Friday, past
            new Calendar { Date = today.AddDays(4), DayOfWeekNumber = 99, IsWorkingDay = true }    // Monday, future
        );
        await context.SaveChangesAsync();

        await repository.UpdateFutureWorkingDaysAsync(DayOfWeek.Friday, false, today);

        var allDays = await context.Calendar.ToListAsync();
        Assert.All(allDays.Where(d => d.Date.DayOfWeek == DayOfWeek.Friday && d.Date > today), d => Assert.False(d.IsWorkingDay));
        Assert.True(Assert.Single(allDays, d => d.Date.DayOfWeek == DayOfWeek.Friday && d.Date < today).IsWorkingDay);
        Assert.True(Assert.Single(allDays, d => d.Date == today).IsWorkingDay);
        Assert.True(Assert.Single(allDays, d => d.Date.DayOfWeek == DayOfWeek.Monday).IsWorkingDay);
    }

    [Fact]
    public async Task UpdateFutureWorkingDaysAsync_CanFlipDayBackToWorking()
    {
        var repository = CreateSut(out var context);
        var today = new DateTime(2026, 9, 24);

        context.Calendar.Add(new Calendar { Date = today.AddDays(1), DayOfWeekNumber = 99, IsWorkingDay = false });
        await context.SaveChangesAsync();

        await repository.UpdateFutureWorkingDaysAsync(DayOfWeek.Friday, true, today);

        var friday = await context.Calendar.SingleAsync();
        Assert.True(friday.IsWorkingDay);
    }
}
