using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Infra.Data.Context;
using Service.Infra.Data.Repositories;

namespace Service.Tests.InfraData;

public class HolidayRepositoryTests
{
    private static HolidayRepository CreateSut(out ApplicationDbContext context)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        context = new ApplicationDbContext(options);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(1);

        return new HolidayRepository(context, currentUser);
    }

    [Fact]
    public async Task GetFilteredAsync_ExcludesPastHolidays_ByDefault()
    {
        var repository = CreateSut(out var context);
        var today = DateTime.UtcNow.Date;
        context.Holiday.AddRange(
            new Holiday { Id = 1, Name = "Past", Date = today.AddDays(-1) },
            new Holiday { Id = 2, Name = "Today", Date = today },
            new Holiday { Id = 3, Name = "Future", Date = today.AddDays(1) });
        context.SaveChanges();

        var result = await repository.GetFilteredAsync(1, 10, includePast: false);

        Assert.Equal(2, result.TotalCount);
        Assert.DoesNotContain(result, h => h.Name == "Past");
    }

    [Fact]
    public async Task GetFilteredAsync_IncludesPastHolidays_WhenIncludePastIsTrue()
    {
        var repository = CreateSut(out var context);
        var today = DateTime.UtcNow.Date;
        context.Holiday.AddRange(
            new Holiday { Id = 1, Name = "Past", Date = today.AddDays(-1) },
            new Holiday { Id = 2, Name = "Today", Date = today });
        context.SaveChanges();

        var result = await repository.GetFilteredAsync(1, 10, includePast: true);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetFilteredAsync_ExcludesSoftDeletedHolidays()
    {
        var repository = CreateSut(out var context);
        var today = DateTime.UtcNow.Date;
        context.Holiday.Add(new Holiday { Id = 1, Name = "Deleted", Date = today, deletedAt = DateTime.UtcNow });
        context.SaveChanges();

        var result = await repository.GetFilteredAsync(1, 10, includePast: true);

        Assert.Empty(result);
    }
}
