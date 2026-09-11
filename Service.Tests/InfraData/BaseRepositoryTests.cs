using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Infra.Data.Context;
using Service.Infra.Data.Repositories;

namespace Service.Tests.InfraData;

public class BaseRepositoryTests
{
    private static (BaseRepository<User> repository, ApplicationDbContext context) CreateSut(int userId = 1)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(userId);

        var repository = new BaseRepository<User>(context, currentUser);
        return (repository, context);
    }

    [Fact]
    public async Task AddAsync_SetsCreatedAtAndCreatedBy()
    {
        var (repository, _) = CreateSut(userId: 42);
        var user = new User { Name = "Matheus", Email = "matheus@test.com", Password = "hash" };

        var result = await repository.AddAsync(user);

        Assert.Equal(42, result.createdBy);
        Assert.True(result.createdAt > DateTime.MinValue);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenEntityIsSoftDeleted()
    {
        var (repository, context) = CreateSut();
        var user = new User { Name = "Matheus", Email = "matheus@test.com", Password = "hash", deletedAt = DateTime.UtcNow };
        context.User.Add(user);
        await context.SaveChangesAsync();

        var result = await repository.GetByIdAsync(user.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ExcludesSoftDeletedAndPaginates()
    {
        var (repository, context) = CreateSut();
        for (var i = 1; i <= 5; i++)
        {
            context.User.Add(new User { Name = $"User {i}", Email = $"user{i}@test.com", Password = "hash" });
        }
        context.User.Add(new User { Name = "Deleted", Email = "deleted@test.com", Password = "hash", deletedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var page1 = await repository.GetAllAsync(pageNumber: 1, pageSize: 2);

        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(2, page1.Count);
    }

    [Fact]
    public async Task UpdateAsync_SetsUpdatedAtAndUpdatedBy()
    {
        var (repository, context) = CreateSut(userId: 99);
        var user = new User { Name = "Matheus", Email = "matheus@test.com", Password = "hash" };
        context.User.Add(user);
        await context.SaveChangesAsync();

        user.Name = "Updated";
        var result = await repository.UpdateAsync(user);

        Assert.Equal("Updated", result.Name);
        Assert.NotNull(result.updatedAt);
        Assert.Equal(99, result.updatedBy);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesInsteadOfRemovingRow()
    {
        var (repository, context) = CreateSut(userId: 7);
        var user = new User { Name = "Matheus", Email = "matheus@test.com", Password = "hash" };
        context.User.Add(user);
        await context.SaveChangesAsync();

        var result = await repository.DeleteAsync(user.Id);

        Assert.NotNull(result);
        Assert.NotNull(result!.deletedAt);
        Assert.Equal(7, result.deletedBy);

        var stillInDb = await context.User.FindAsync(user.Id);
        Assert.NotNull(stillInDb);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsNull_WhenEntityDoesNotExist()
    {
        var (repository, _) = CreateSut();

        var result = await repository.DeleteAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task Exists_ReturnsFalse_ForSoftDeletedEntity()
    {
        var (repository, context) = CreateSut();
        var user = new User { Name = "Matheus", Email = "matheus@test.com", Password = "hash", deletedAt = DateTime.UtcNow };
        context.User.Add(user);
        await context.SaveChangesAsync();

        var exists = await repository.Exists(user.Id);

        Assert.False(exists);
    }

    [Fact]
    public async Task Exists_ReturnsTrue_ForActiveEntity()
    {
        var (repository, context) = CreateSut();
        var user = new User { Name = "Matheus", Email = "matheus@test.com", Password = "hash" };
        context.User.Add(user);
        await context.SaveChangesAsync();

        var exists = await repository.Exists(user.Id);

        Assert.True(exists);
    }
}
