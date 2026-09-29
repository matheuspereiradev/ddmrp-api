using Microsoft.EntityFrameworkCore;
using Service.Domain.Entities;
using Service.Infra.Data.Context;
using Service.Infra.Data.Repositories;

namespace Service.Tests.InfraData;

public class TableLayoutRepositoryTests
{
    private static (ApplicationDbContext Context, TableLayoutRepository Repository) CreateSut()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);

        context.User.Add(new User { Id = 1, Name = "User 1", Email = "user1@test.com", Password = "hash", IdRole = 1 });
        context.User.Add(new User { Id = 2, Name = "User 2", Email = "user2@test.com", Password = "hash", IdRole = 1 });
        context.SaveChanges();

        return (context, new TableLayoutRepository(context));
    }

    private static TableLayout Layout(int userId, string tableName, params string[] columnIds) => new()
    {
        UserId = userId,
        TableName = tableName,
        Columns = columnIds.Select((id, i) => new TableLayoutColumn { ColumnId = id, Order = i, Visible = true }).ToList(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task GetAsync_ReturnsLayoutWithColumns_ForMatchingUserAndTable()
    {
        var (_, repository) = CreateSut();
        await repository.AddAsync(Layout(1, "inventoryBufferManagement", "a", "b"));

        var result = await repository.GetAsync(1, "inventoryBufferManagement");

        Assert.NotNull(result);
        Assert.Equal(["a", "b"], result.Columns.Select(c => c.ColumnId));
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenLayoutBelongsToADifferentUser()
    {
        var (_, repository) = CreateSut();
        await repository.AddAsync(Layout(2, "inventoryBufferManagement", "a"));

        var result = await repository.GetAsync(1, "inventoryBufferManagement");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenTableNameDiffers()
    {
        var (_, repository) = CreateSut();
        await repository.AddAsync(Layout(1, "inventoryBufferManagement", "a"));

        var result = await repository.GetAsync(1, "inventoryBufferManagement.inbounds");

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ReplacesColumns()
    {
        var (_, repository) = CreateSut();
        await repository.AddAsync(Layout(1, "t", "a", "b"));
        var layout = await repository.GetAsync(1, "t");

        layout!.Columns = [new TableLayoutColumn { ColumnId = "c", Order = 0, Visible = false }];
        await repository.UpdateAsync(layout);

        var result = await repository.GetAsync(1, "t");
        Assert.Equal(["c"], result!.Columns.Select(c => c.ColumnId));
    }

    [Fact]
    public async Task DeleteAsync_RemovesRow()
    {
        var (context, repository) = CreateSut();
        await repository.AddAsync(Layout(1, "t", "a"));
        var layout = await repository.GetAsync(1, "t");

        await repository.DeleteAsync(layout!);

        Assert.Equal(0, await context.TableLayout.CountAsync());
    }
}
