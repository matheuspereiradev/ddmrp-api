using Microsoft.EntityFrameworkCore;
using Service.Domain.Entities;
using Service.Infra.Data.Context;
using Service.Infra.Data.Repositories;

namespace Service.Tests.InfraData;

public class UserCenterRepositoryTests
{
    private static (UserCenterRepository repository, ApplicationDbContext context) CreateSut()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);
        return (new UserCenterRepository(context), context);
    }

    private static async Task<(User user, List<Center> centers)> SeedAsync(ApplicationDbContext context, bool hasAllCentersAccess = false)
    {
        var user = new User { Name = "User", Email = "user@test.com", Password = "hash", HasAllCentersAccess = hasAllCentersAccess };
        var centers = new List<Center>
        {
            new() { Code = "C1", Description = "Center 1" },
            new() { Code = "C2", Description = "Center 2" },
            new() { Code = "C3", Description = "Center 3" },
            new() { Code = "C4", Description = "Deleted", deletedAt = DateTime.UtcNow }
        };
        context.User.Add(user);
        context.Center.AddRange(centers);
        await context.SaveChangesAsync();
        return (user, centers);
    }

    [Fact]
    public async Task GetAllowedCenterIdsAsync_ReturnsNull_WhenUserHasAllCentersAccess()
    {
        var (repository, context) = CreateSut();
        var (user, centers) = await SeedAsync(context, hasAllCentersAccess: true);
        context.UserCenter.Add(new UserCenter { IdUser = user.Id, IdCenter = centers[0].Id });
        await context.SaveChangesAsync();

        var result = await repository.GetAllowedCenterIdsAsync(user.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllowedCenterIdsAsync_ReturnsEmpty_WhenUserHasNoCentersAssigned()
    {
        var (repository, context) = CreateSut();
        var (user, _) = await SeedAsync(context);

        var result = await repository.GetAllowedCenterIdsAsync(user.Id);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllowedCenterIdsAsync_ReturnsOnlyAssignedAndNotDeletedCenters()
    {
        var (repository, context) = CreateSut();
        var (user, centers) = await SeedAsync(context);
        var otherUser = new User { Name = "Other", Email = "other@test.com", Password = "hash" };
        context.User.Add(otherUser);
        await context.SaveChangesAsync();
        context.UserCenter.AddRange(
            new UserCenter { IdUser = user.Id, IdCenter = centers[1].Id },
            new UserCenter { IdUser = user.Id, IdCenter = centers[2].Id },
            new UserCenter { IdUser = user.Id, IdCenter = centers[3].Id },
            new UserCenter { IdUser = otherUser.Id, IdCenter = centers[0].Id });
        await context.SaveChangesAsync();

        var result = await repository.GetAllowedCenterIdsAsync(user.Id);

        Assert.Equal(new[] { centers[1].Id, centers[2].Id }, result!.OrderBy(id => id));
    }

    [Fact]
    public async Task GetAccessibleCenterIdsAsync_ReturnsAllNotDeletedCenterIds_WhenUserHasAllCentersAccess()
    {
        var (repository, context) = CreateSut();
        var (user, centers) = await SeedAsync(context, hasAllCentersAccess: true);

        var result = await repository.GetAccessibleCenterIdsAsync(user.Id);

        Assert.Equal(new[] { centers[0].Id, centers[1].Id, centers[2].Id }, result);
    }

    [Fact]
    public async Task GetAccessibleCenterIdsAsync_ReturnsAssignedNotDeletedCenterIds_WhenUserDoesNotHaveAllCentersAccess()
    {
        var (repository, context) = CreateSut();
        var (user, centers) = await SeedAsync(context);
        context.UserCenter.AddRange(
            new UserCenter { IdUser = user.Id, IdCenter = centers[2].Id },
            new UserCenter { IdUser = user.Id, IdCenter = centers[3].Id });
        await context.SaveChangesAsync();

        var result = await repository.GetAccessibleCenterIdsAsync(user.Id);

        Assert.Equal(new[] { centers[2].Id }, result);
    }

    [Fact]
    public async Task GetAssignedCentersAsync_IgnoresAllCentersAccessFlag()
    {
        var (repository, context) = CreateSut();
        var (user, centers) = await SeedAsync(context, hasAllCentersAccess: true);
        context.UserCenter.Add(new UserCenter { IdUser = user.Id, IdCenter = centers[0].Id });
        await context.SaveChangesAsync();

        var result = await repository.GetAssignedCentersAsync(user.Id);

        Assert.Equal(new[] { "C1" }, result.Select(c => c.Code));
    }

    [Fact]
    public async Task ReplaceAsync_AddsMissingAndRemovesStaleAssignments()
    {
        var (repository, context) = CreateSut();
        var (user, centers) = await SeedAsync(context);
        context.UserCenter.AddRange(
            new UserCenter { IdUser = user.Id, IdCenter = centers[0].Id },
            new UserCenter { IdUser = user.Id, IdCenter = centers[1].Id });
        await context.SaveChangesAsync();

        await repository.ReplaceAsync(user.Id, new[] { centers[1].Id, centers[2].Id });

        var stored = await context.UserCenter.Where(uc => uc.IdUser == user.Id).Select(uc => uc.IdCenter).ToListAsync();
        Assert.Equal(new[] { centers[1].Id, centers[2].Id }, stored.OrderBy(id => id));
    }

    [Fact]
    public async Task ReplaceAsync_WithEmptyList_ClearsAssignments_WithoutTouchingOtherUsers()
    {
        var (repository, context) = CreateSut();
        var (user, centers) = await SeedAsync(context);
        var otherUser = new User { Name = "Other", Email = "other@test.com", Password = "hash" };
        context.User.Add(otherUser);
        await context.SaveChangesAsync();
        context.UserCenter.AddRange(
            new UserCenter { IdUser = user.Id, IdCenter = centers[0].Id },
            new UserCenter { IdUser = otherUser.Id, IdCenter = centers[0].Id });
        await context.SaveChangesAsync();

        await repository.ReplaceAsync(user.Id, Array.Empty<int>());

        Assert.Empty(await context.UserCenter.Where(uc => uc.IdUser == user.Id).ToListAsync());
        Assert.Single(await context.UserCenter.Where(uc => uc.IdUser == otherUser.Id).ToListAsync());
    }
}
