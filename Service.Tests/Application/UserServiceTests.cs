using NSubstitute;
using Service.Application.DTOs.User;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;

namespace Service.Tests.Application;

public class UserServiceTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _roleRepository.Exists(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        _sut = new UserService(_userRepository, _roleRepository);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenUserExists()
    {
        var user = new User { Id = 1, Name = "Matheus", Email = "matheus@test.com" };
        _userRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.GetByIdAsync(1);

        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.Name, result.Name);
        Assert.Equal(user.Email, result.Email);
    }

    [Fact]
    public async Task GetByIdAsync_IncludesRole_WhenLoaded()
    {
        var user = new User { Id = 1, Name = "Matheus", Email = "matheus@test.com", Role = new Role { Id = 1, Name = "Admin" } };
        _userRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.GetByIdAsync(1);

        Assert.NotNull(result.Role);
        Assert.Equal("Admin", result.Role!.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFoundException_WhenUserDoesNotExist()
    {
        _userRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((User)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(1));
    }

    [Fact]
    public async Task GetAllAsync_MapsPagedResultToDto()
    {
        var users = new List<User>
        {
            new() { Id = 1, Name = "A", Email = "a@test.com" },
            new() { Id = 2, Name = "B", Email = "b@test.com" }
        };
        var paged = new PagedList<User>(users, pageNumber: 1, pageSize: 10, count: 2);
        _userRepository.GetAllAsync(1, 10, Arg.Any<CancellationToken>()).Returns(paged);

        var result = await _sut.GetAllAsync(1, 10);

        Assert.Equal(2, result.Count);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal("A", result[0].Name);
    }

    [Fact]
    public async Task AddAsync_ThrowsBadRequestException_WhenEmailAlreadyInUse()
    {
        var postDto = new UserPostDto { Name = "Matheus", Email = "matheus@test.com", Password = "12345678" };
        _userRepository.GetByEmail(postDto.Email, Arg.Any<CancellationToken>())
            .Returns(new User { Id = 1, Email = postDto.Email });

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddAsync(postDto));

        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_ThrowsBadRequestException_WhenRoleDoesNotExist()
    {
        var postDto = new UserPostDto { Name = "Matheus", Email = "matheus@test.com", Password = "12345678", IdRole = 99 };
        _userRepository.GetByEmail(postDto.Email, Arg.Any<CancellationToken>()).Returns((User)null!);
        _roleRepository.Exists(99, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddAsync(postDto));

        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_HashesPasswordAndPersists_WhenEmailIsFree()
    {
        var postDto = new UserPostDto { Name = "Matheus", Email = "matheus@test.com", Password = "12345678" };
        _userRepository.GetByEmail(postDto.Email, Arg.Any<CancellationToken>()).Returns((User)null!);
        _userRepository.AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<User>());

        var result = await _sut.AddAsync(postDto);

        Assert.Equal(postDto.Name, result.Name);
        await _userRepository.Received(1).AddAsync(
            Arg.Is<User>(u => u.Email == postDto.Email
                && u.Password != postDto.Password
                && BCrypt.Net.BCrypt.Verify(postDto.Password, u.Password)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_AppliesChangesAndPersists_WhenUserExists()
    {
        var existing = new User { Id = 1, Name = "Old", Email = "old@test.com" };
        var putDto = new UserPutDto { Name = "New", Email = "new@test.com" };
        _userRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _userRepository.UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<User>());

        var result = await _sut.UpdateAsync(1, putDto);

        Assert.Equal("New", result.Name);
        Assert.Equal("new@test.com", result.Email);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsNotFoundException_WhenUserDoesNotExist()
    {
        _userRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((User)null!);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UpdateAsync(1, new UserPutDto { Name = "X", Email = "x@test.com" }));
    }

    [Fact]
    public async Task UpdateAsync_ThrowsBadRequestException_WhenRoleDoesNotExist()
    {
        var putDto = new UserPutDto { Name = "New", Email = "new@test.com", IdRole = 99 };
        _roleRepository.Exists(99, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.UpdateAsync(1, putDto));

        await _userRepository.DidNotReceive().GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ReturnsDeletedDto_WhenUserExists()
    {
        var deleted = new User { Id = 1, Name = "Matheus", Email = "matheus@test.com" };
        _userRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns(deleted);

        var result = await _sut.DeleteAsync(1);

        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsNotFoundException_WhenUserDoesNotExist()
    {
        _userRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns((User)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(1));
    }

    [Fact]
    public async Task GetUserByEmail_ReturnsDto_WhenFound()
    {
        var user = new User { Id = 1, Name = "Matheus", Email = "matheus@test.com" };
        _userRepository.GetByEmail(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.GetUserByEmail(user.Email);

        Assert.Equal(user.Email, result.Email);
    }

    [Fact]
    public async Task GetUserByEmail_ThrowsNotFoundException_WhenNotFound()
    {
        _userRepository.GetByEmail("missing@test.com", Arg.Any<CancellationToken>()).Returns((User)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetUserByEmail("missing@test.com"));
    }
}
