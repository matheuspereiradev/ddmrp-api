using NSubstitute;
using Service.Application.DTOs.Role;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Tests.Application;

public class RoleServiceTests
{
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly RoleService _sut;

    public RoleServiceTests()
    {
        _sut = new RoleService(_roleRepository);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenRoleExists()
    {
        var role = new Role { Id = 1, Name = "Admin" };
        _roleRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(role);

        var result = await _sut.GetByIdAsync(1);

        Assert.Equal(role.Id, result.Id);
        Assert.Equal(role.Name, result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFoundException_WhenRoleDoesNotExist()
    {
        _roleRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Role)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(1));
    }

    [Fact]
    public async Task AddAsync_PersistsMappedEntity()
    {
        var postDto = new RolePostDto { Name = "Admin" };
        _roleRepository.AddAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Role>());

        var result = await _sut.AddAsync(postDto);

        Assert.Equal("Admin", result.Name);
        await _roleRepository.Received(1).AddAsync(
            Arg.Is<Role>(r => r.Name == "Admin"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_AppliesChanges_WhenRoleExists()
    {
        var existing = new Role { Id = 1, Name = "Old" };
        var putDto = new RolePutDto { Name = "New" };
        _roleRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _roleRepository.UpdateAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Role>());

        var result = await _sut.UpdateAsync(1, putDto);

        Assert.Equal("New", result.Name);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsNotFoundException_WhenRoleDoesNotExist()
    {
        _roleRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns((Role)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(1));
    }
}
