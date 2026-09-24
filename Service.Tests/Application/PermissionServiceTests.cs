using NSubstitute;
using Service.Application.DTOs.Permission;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Tests.Application;

public class PermissionServiceTests
{
    private readonly IPermissionRepository _permissionRepository = Substitute.For<IPermissionRepository>();
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly PermissionService _sut;

    public PermissionServiceTests()
    {
        _sut = new PermissionService(_permissionRepository, _roleRepository);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedPermissions()
    {
        _permissionRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Permission>
        {
            new() { Id = "api/role:GET", Description = "List roles", Module = "Role" }
        });

        var result = await _sut.GetAllAsync();

        Assert.Single(result);
        Assert.Equal("api/role:GET", result[0].Id);
        Assert.Equal("Role", result[0].Module);
    }

    [Fact]
    public async Task GetByRoleAsync_ThrowsNotFoundException_WhenRoleDoesNotExist()
    {
        _roleRepository.Exists(1, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByRoleAsync(1));
    }

    [Fact]
    public async Task ReplaceRolePermissionsAsync_ThrowsNotFoundException_WhenRoleDoesNotExist()
    {
        _roleRepository.Exists(1, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.ReplaceRolePermissionsAsync(1, new ReplaceRolePermissionsDto { PermissionIds = ["api/role:GET"] }));
    }

    [Fact]
    public async Task ReplaceRolePermissionsAsync_ThrowsBadRequestException_WhenPermissionIdIsUnknown()
    {
        _roleRepository.Exists(1, Arg.Any<CancellationToken>()).Returns(true);
        _permissionRepository.GetExistingIdsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<string>());

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _sut.ReplaceRolePermissionsAsync(1, new ReplaceRolePermissionsDto { PermissionIds = ["api/role:GET"] }));

        await _permissionRepository.DidNotReceive().ReplaceRolePermissionsAsync(
            Arg.Any<int>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplaceRolePermissionsAsync_ReplacesPermissions_WhenAllIdsAreKnown()
    {
        _roleRepository.Exists(1, Arg.Any<CancellationToken>()).Returns(true);
        _permissionRepository.GetExistingIdsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<string> { "api/role:GET", "api/role:POST" });

        await _sut.ReplaceRolePermissionsAsync(1, new ReplaceRolePermissionsDto { PermissionIds = ["api/role:GET", "api/role:POST"] });

        await _permissionRepository.Received(1).ReplaceRolePermissionsAsync(
            1,
            Arg.Is<List<string>>(ids => ids.Count == 2 && ids.Contains("api/role:GET") && ids.Contains("api/role:POST")),
            Arg.Any<CancellationToken>());
    }
}
