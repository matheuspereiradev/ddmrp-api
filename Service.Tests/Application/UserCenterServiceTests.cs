using NSubstitute;
using Service.Application.DTOs.UserCenter;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Tests.Application;

public class UserCenterServiceTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUserCenterRepository _userCenterRepository = Substitute.For<IUserCenterRepository>();
    private readonly ICenterRepository _centerRepository = Substitute.For<ICenterRepository>();
    private readonly UserCenterService _sut;

    public UserCenterServiceTests()
    {
        _sut = new UserCenterService(_userRepository, _userCenterRepository, _centerRepository);
    }

    private void GivenUser(int id = 1, bool hasAllCentersAccess = false)
    {
        _userRepository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(new User { Id = id, Name = "User", Email = "user@test.com", HasAllCentersAccess = hasAllCentersAccess });
    }

    private void GivenNoUser(int id)
    {
        _userRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((User)null!);
    }

    [Fact]
    public async Task GetAssignedAsync_ThrowsNotFound_WhenUserDoesNotExist()
    {
        GivenNoUser(99);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetAssignedAsync(99));
    }

    [Fact]
    public async Task GetAssignedAsync_ReturnsFlagAndAssignedCenters()
    {
        GivenUser(hasAllCentersAccess: false);
        _userCenterRepository.GetAssignedCentersAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<Center> { new() { Id = 2, Code = "C2", Description = "Center 2" } });

        var result = await _sut.GetAssignedAsync(1);

        Assert.Equal(1, result.IdUser);
        Assert.False(result.HasAllCentersAccess);
        Assert.Equal(new[] { "C2" }, result.Centers.Select(c => c.Code));
    }

    [Fact]
    public async Task GetAccessibleAsync_ReturnsFlagAndEffectiveCenterIds()
    {
        GivenUser(hasAllCentersAccess: true);
        _userCenterRepository.GetAccessibleCenterIdsAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<int> { 1, 2 });

        var result = await _sut.GetAccessibleAsync(1);

        Assert.Equal(1, result.IdUser);
        Assert.True(result.HasAllCentersAccess);
        Assert.Equal(new[] { 1, 2 }, result.Centers);
        await _userCenterRepository.DidNotReceive().GetAssignedCentersAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAccessibleAsync_ThrowsNotFound_WhenUserDoesNotExist()
    {
        GivenNoUser(99);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetAccessibleAsync(99));
    }

    [Fact]
    public async Task SetCentersAsync_ThrowsBadRequest_WhenAnyCenterDoesNotExist()
    {
        GivenUser();
        _centerRepository.GetExistingIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<int> { 1 });

        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            _sut.SetCentersAsync(1, new UserCentersPutDto { CenterIds = new List<int> { 1, 7 } }));

        Assert.Contains("7", ex.Message);
        await _userCenterRepository.DidNotReceive().ReplaceAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetCentersAsync_ReplacesWithDistinctIds()
    {
        GivenUser();
        _centerRepository.GetExistingIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<int> { 1, 2 });
        _userCenterRepository.GetAssignedCentersAsync(1, Arg.Any<CancellationToken>()).Returns(new List<Center>());

        await _sut.SetCentersAsync(1, new UserCentersPutDto { CenterIds = new List<int> { 1, 2, 2, 1 } });

        await _userCenterRepository.Received(1).ReplaceAsync(
            1,
            Arg.Is<IReadOnlyCollection<int>>(ids => ids.OrderBy(id => id).SequenceEqual(new[] { 1, 2 })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetCentersAsync_ThrowsNotFound_WhenUserDoesNotExist()
    {
        GivenNoUser(99);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.SetCentersAsync(99, new UserCentersPutDto { CenterIds = new List<int>() }));
    }

    [Fact]
    public async Task SetAllCentersAccessAsync_UpdatesOnlyTheFlag()
    {
        var user = new User { Id = 1, Name = "User", Email = "user@test.com", HasAllCentersAccess = false };
        _userRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        _userCenterRepository.GetAssignedCentersAsync(1, Arg.Any<CancellationToken>()).Returns(new List<Center>());

        var result = await _sut.SetAllCentersAccessAsync(1, true);

        Assert.True(result.HasAllCentersAccess);
        await _userRepository.Received(1).UpdateAsync(Arg.Is<User>(u => u.Id == 1 && u.HasAllCentersAccess), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetAllCentersAccessAsync_ThrowsNotFound_WhenUserDoesNotExist()
    {
        GivenNoUser(99);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.SetAllCentersAccessAsync(99, true));
    }
}
