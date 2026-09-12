using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Infra.Data.Context;
using Service.Infra.Data.Repositories;

namespace Service.Tests.InfraData;

public class UserRepositoryTests
{
    private static (UserRepository repository, ApplicationDbContext context) CreateSut()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(1);

        var repository = new UserRepository(context, currentUser);
        return (repository, context);
    }

    [Fact]
    public async Task GetByEmail_ReturnsUser_WhenFound()
    {
        var (repository, context) = CreateSut();
        var role = new Role { Name = "Admin" };
        context.Role.Add(role);
        await context.SaveChangesAsync();
        var user = new User { Name = "Matheus", Email = "matheus@test.com", Password = "hash", IdRole = role.Id };
        context.User.Add(user);
        await context.SaveChangesAsync();

        var result = await repository.GetByEmail("matheus@test.com");

        Assert.NotNull(result);
        Assert.Equal(user.Id, result!.Id);
        Assert.NotNull(result.Role);
        Assert.Equal("Admin", result.Role.Name);
    }

    [Fact]
    public async Task GetByEmail_ReturnsNull_WhenSoftDeleted()
    {
        var (repository, context) = CreateSut();
        var role = new Role { Name = "Admin" };
        context.Role.Add(role);
        await context.SaveChangesAsync();
        var user = new User { Name = "Matheus", Email = "matheus@test.com", Password = "hash", IdRole = role.Id, deletedAt = DateTime.UtcNow };
        context.User.Add(user);
        await context.SaveChangesAsync();

        var result = await repository.GetByEmail("matheus@test.com");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByEmail_ReturnsNull_WhenNotFound()
    {
        var (repository, _) = CreateSut();

        var result = await repository.GetByEmail("missing@test.com");

        Assert.Null(result);
    }
}
