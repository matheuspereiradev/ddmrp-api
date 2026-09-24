using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Infra.Data.Context;
using Service.Infra.Data.Repositories;

namespace Service.Tests.InfraData;

public class ForecastRepositoryTests
{
    // January 2026 starts on a Thursday: 31 days = 22 business days (Mon-Fri) + 9 weekend days,
    // per the seeded Calendar.IsWorkingDay flags below (Mon-Fri true, Sat/Sun false).
    private const int JanuaryDays = 31;
    private const int JanuaryBusinessDays = 22;

    private static ForecastRepository CreateSut(out ApplicationDbContext context)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        context = new ApplicationDbContext(options);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(1);

        if (!context.Calendar.Any())
        {
            var dates = new List<Calendar>();
            for (var date = new DateTime(2026, 1, 1); date <= new DateTime(2026, 2, 28); date = date.AddDays(1))
                dates.Add(new Calendar
                {
                    Date = date,
                    DayOfWeekNumber = (int)date.DayOfWeek,
                    IsWorkingDay = date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)
                });
            context.Calendar.AddRange(dates);
        }

        context.Product.AddRange(
            new Product { Id = 1, Reference = "REF1", Description = "Product 1", UnitOfMeasure = "UN" },
            new Product { Id = 2, Reference = "REF2", Description = "Product 2", UnitOfMeasure = "UN" });
        context.Center.AddRange(
            new Center { Id = 1, Code = "C1", Description = "Center 1" },
            new Center { Id = 2, Code = "C2", Description = "Center 2" });

        context.SaveChanges();

        return new ForecastRepository(context, currentUser);
    }

    [Fact]
    public async Task GetFilteredAsync_ExplodesForecastIntoOneRowPerCalendarDay_ZeroingNonBusinessDays()
    {
        var repository = CreateSut(out var context);
        context.Forecast.Add(new Forecast { Id = 1, IdProduct = 1, IdCenter = 1, Value = 220m, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 1, 31) });
        context.SaveChanges();

        var result = await repository.GetFilteredAsync(null, null, null, null, 1, 100);

        Assert.Equal(JanuaryDays, result.TotalCount);
        Assert.Equal(JanuaryBusinessDays, result.Count(r => r.Value != 0));
        Assert.All(result.Where(r => r.Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday), r => Assert.Equal(0m, r.Value));
        Assert.Equal(10m, Assert.Single(result, r => r.Date == new DateTime(2026, 1, 5)).Value);
    }

    [Fact]
    public async Task GetFilteredAsync_TreatsHolidayDateAsNonBusinessDay_EvenWhenCalendarMarksItWorkingDay()
    {
        var repository = CreateSut(out var context);
        context.Forecast.Add(new Forecast { Id = 1, IdProduct = 1, IdCenter = 1, Value = 220m, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 1, 31) });
        context.Holiday.Add(new Holiday { Id = 1, Name = "New Year (observed)", Date = new DateTime(2026, 1, 5) });
        context.SaveChanges();

        var result = await repository.GetFilteredAsync(null, null, null, null, 1, 100);

        Assert.Equal(JanuaryBusinessDays - 1, result.Count(r => r.Value != 0));
        Assert.Equal(0m, Assert.Single(result, r => r.Date == new DateTime(2026, 1, 5)).Value);
    }

    [Fact]
    public async Task GetFilteredAsync_FiltersExplodedRowsByDateRange()
    {
        var repository = CreateSut(out var context);
        context.Forecast.Add(new Forecast { Id = 1, IdProduct = 1, IdCenter = 1, Value = 220m, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 1, 31) });
        context.SaveChanges();

        var result = await repository.GetFilteredAsync(null, null, new DateTime(2026, 1, 1), new DateTime(2026, 1, 9), 1, 100);

        // Jan 1 (Thu) through Jan 9 (Fri): every calendar day counts, business or not = 9 days.
        Assert.Equal(9, result.TotalCount);
        Assert.All(result, r => Assert.True(r.Date >= new DateTime(2026, 1, 1) && r.Date <= new DateTime(2026, 1, 9)));
    }

    [Fact]
    public async Task GetFilteredAsync_FiltersByIdProductAndIdCenter()
    {
        var repository = CreateSut(out var context);
        context.Forecast.AddRange(
            new Forecast { Id = 1, IdProduct = 1, IdCenter = 1, Value = 220m, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 1, 31) },
            new Forecast { Id = 2, IdProduct = 2, IdCenter = 2, Value = 220m, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 1, 31) });
        context.SaveChanges();

        var result = await repository.GetFilteredAsync(2, 2, null, null, 1, 100);

        Assert.Equal(JanuaryDays, result.TotalCount);
        Assert.All(result, r => Assert.Equal(2, r.IdProduct));
    }

    [Fact]
    public async Task GetGroupedAsync_ReturnsRawIntervalRecords_NotExploded()
    {
        var repository = CreateSut(out var context);
        context.Forecast.Add(new Forecast { Id = 1, IdProduct = 1, IdCenter = 1, Value = 220m, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 1, 31) });
        context.SaveChanges();

        var result = await repository.GetGroupedAsync(null, null, null, null, 1, 100);

        var item = Assert.Single(result);
        Assert.Equal(220m, item.Value);
        Assert.Equal(new DateTime(2026, 1, 1), item.StartDate);
        Assert.Equal(new DateTime(2026, 1, 31), item.EndDate);
    }

    [Fact]
    public async Task GetGroupedAsync_FiltersByOverlappingDateRange()
    {
        var repository = CreateSut(out var context);
        context.Forecast.AddRange(
            new Forecast { Id = 1, IdProduct = 1, IdCenter = 1, Value = 220m, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 1, 31) },
            new Forecast { Id = 2, IdProduct = 1, IdCenter = 1, Value = 200m, StartDate = new DateTime(2026, 2, 1), EndDate = new DateTime(2026, 2, 28) });
        context.SaveChanges();

        var result = await repository.GetGroupedAsync(null, null, new DateTime(2026, 2, 1), new DateTime(2026, 2, 28), 1, 100);

        Assert.Equal(200m, Assert.Single(result).Value);
    }
}
