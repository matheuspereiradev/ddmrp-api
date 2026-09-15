using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Infra.Data.Context;
using Service.Infra.Data.Repositories;

namespace Service.Tests.InfraData;

public class WorkspaceRepositoryTests
{
    private static (ApplicationDbContext Context, WorkspaceRepository Repository) CreateSut(int currentUserId = 1)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(currentUserId);

        context.Center.Add(new Center { Id = 1, Code = "C1", Description = "Center 1" });
        context.Product.Add(new Product { Id = 1, Reference = "REF1", Description = "Product 1", UnitOfMeasure = "UN" });
        context.User.Add(new User { Id = 1, Name = "User 1", Email = "user1@test.com", Password = "hash", IdRole = 1 });
        context.User.Add(new User { Id = 2, Name = "User 2", Email = "user2@test.com", Password = "hash", IdRole = 1 });
        context.SaveChanges();

        return (context, new WorkspaceRepository(context, currentUser));
    }

    [Fact]
    public async Task GetByKeyAsync_ReturnsWorkspace_WhenKeyMatchesForThatUser()
    {
        var (context, repository) = CreateSut();
        context.Workspace.Add(new Workspace { IdCenter = 1, IdProduct = 1, IdUser = 1, OptimizedQuantity = 10, Approved = true });
        context.SaveChanges();

        var result = await repository.GetByKeyAsync(1, 1, 1);

        Assert.NotNull(result);
        Assert.Equal(10, result.OptimizedQuantity);
    }

    [Fact]
    public async Task GetByKeyAsync_ReturnsNull_WhenKeyBelongsToADifferentUser()
    {
        var (context, repository) = CreateSut();
        context.Workspace.Add(new Workspace { IdCenter = 1, IdProduct = 1, IdUser = 2, OptimizedQuantity = 10, Approved = true });
        context.SaveChanges();

        var result = await repository.GetByKeyAsync(1, 1, 1);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByKeyAsync_IgnoresSoftDeletedWorkspaces()
    {
        var (context, repository) = CreateSut();
        context.Workspace.Add(new Workspace { IdCenter = 1, IdProduct = 1, IdUser = 1, OptimizedQuantity = 10, Approved = true, deletedAt = DateTime.UtcNow });
        context.SaveChanges();

        var result = await repository.GetByKeyAsync(1, 1, 1);

        Assert.Null(result);
    }

    [Fact]
    public async Task ClearByUserAsync_SoftDeletesOnlyWorkspacesForThatUser()
    {
        var (context, repository) = CreateSut(currentUserId: 1);
        context.Workspace.AddRange(
            new Workspace { Id = 1, IdCenter = 1, IdProduct = 1, IdUser = 1, OptimizedQuantity = 10, Approved = true },
            new Workspace { Id = 2, IdCenter = 1, IdProduct = 1, IdUser = 2, OptimizedQuantity = 20, Approved = false });
        context.SaveChanges();

        await repository.ClearByUserAsync(1);

        var mine = await context.Workspace.FindAsync(1);
        var other = await context.Workspace.FindAsync(2);
        Assert.NotNull(mine!.deletedAt);
        Assert.Equal(1, mine.deletedBy);
        Assert.Null(other!.deletedAt);
    }

    [Fact]
    public async Task ClearByUserAsync_DoesNothing_WhenUserHasNoWorkspaces()
    {
        var (_, repository) = CreateSut();

        await repository.ClearByUserAsync(1);
    }
}
