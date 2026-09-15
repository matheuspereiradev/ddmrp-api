using NSubstitute;
using Service.Application.DTOs.Workspace;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Enums;
using Service.Domain.Interfaces;
using Service.Domain.Report.Results;

namespace Service.Tests.Application;

public class WorkspaceServiceTests
{
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();
    private readonly ICenterRepository _centerRepository = Substitute.For<ICenterRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IReportRepository _reportRepository = Substitute.For<IReportRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly WorkspaceService _sut;

    public WorkspaceServiceTests()
    {
        _centerRepository.Exists(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        _productRepository.Exists(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        _currentUser.UserId.Returns(1);
        _sut = new WorkspaceService(_workspaceRepository, _centerRepository, _productRepository, _reportRepository, _currentUser);
    }

    [Fact]
    public async Task UpdateWorkspaceAsync_CreatesNewWorkspace_WhenNoneExistsForKey()
    {
        var dto = new WorkspaceUpdateDto { IdCenter = 1, IdProduct = 2, OptimizedQuantity = 10, Approved = true };
        _workspaceRepository.GetByKeyAsync(1, 2, 1, Arg.Any<CancellationToken>()).Returns((Workspace)null!);
        _workspaceRepository.AddAsync(Arg.Any<Workspace>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Workspace>());

        var result = await _sut.UpdateWorkspaceAsync(dto);

        // IReportRepository.GetInventoryBufferManagementRowAsync isn't stubbed, so it returns null (no matching CenterProduct) —
        // the simulated fields should fall back to "no buffer" defaults instead of throwing.
        Assert.Equal(0m, result.SimulatedNetflowBufferPercentage);
        Assert.Equal(BufferColor.NoColor, result.SimulatedNetflowBufferColor);
        await _workspaceRepository.Received(1).AddAsync(
            Arg.Is<Workspace>(w => w.IdCenter == 1 && w.IdProduct == 2 && w.IdUser == 1),
            Arg.Any<CancellationToken>());
        await _workspaceRepository.DidNotReceive().UpdateAsync(Arg.Any<Workspace>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateWorkspaceAsync_UpdatesExistingWorkspace_WhenKeyAlreadyExistsForCurrentUser()
    {
        var dto = new WorkspaceUpdateDto { IdCenter = 1, IdProduct = 2, OptimizedQuantity = 20, Approved = false };
        var existing = new Workspace { Id = 5, IdCenter = 1, IdProduct = 2, IdUser = 1, OptimizedQuantity = 10, Approved = true };
        _workspaceRepository.GetByKeyAsync(1, 2, 1, Arg.Any<CancellationToken>()).Returns(existing);
        _workspaceRepository.UpdateAsync(Arg.Any<Workspace>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Workspace>());

        _reportRepository.GetInventoryBufferManagementRowAsync(1, 2, Arg.Any<CancellationToken>())
            .Returns(new InventoryBufferManagementRow
            {
                IdCenter = 1,
                IdProduct = 2,
                SimulatedNetflowBufferPercentage = 0.054m,
                SimulatedNetflowBufferColor = BufferColor.Red
            });

        var result = await _sut.UpdateWorkspaceAsync(dto);

        Assert.Equal(0.054m, result.SimulatedNetflowBufferPercentage);
        Assert.Equal(BufferColor.Red, result.SimulatedNetflowBufferColor);
        await _workspaceRepository.Received(1).UpdateAsync(
            Arg.Is<Workspace>(w => w.Id == 5 && w.OptimizedQuantity == 20 && !w.Approved),
            Arg.Any<CancellationToken>());
        await _workspaceRepository.DidNotReceive().AddAsync(Arg.Any<Workspace>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateWorkspaceAsync_ReturnsSimulatedNetflowBufferFields_FromReportRepository()
    {
        var dto = new WorkspaceUpdateDto { IdCenter = 1, IdProduct = 2, OptimizedQuantity = 10, Approved = true };
        _workspaceRepository.GetByKeyAsync(1, 2, 1, Arg.Any<CancellationToken>()).Returns((Workspace)null!);
        _workspaceRepository.AddAsync(Arg.Any<Workspace>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Workspace>());
        _reportRepository.GetInventoryBufferManagementRowAsync(1, 2, Arg.Any<CancellationToken>())
            .Returns(new InventoryBufferManagementRow
            {
                IdCenter = 1,
                IdProduct = 2,
                SimulatedNetflowBufferPercentage = 0.42m,
                SimulatedNetflowBufferColor = BufferColor.Yellow
            });

        var result = await _sut.UpdateWorkspaceAsync(dto);

        Assert.Equal(0.42m, result.SimulatedNetflowBufferPercentage);
        Assert.Equal(BufferColor.Yellow, result.SimulatedNetflowBufferColor);
    }

    [Fact]
    public async Task UpdateWorkspaceAsync_ThrowsBadRequestException_WhenCenterDoesNotExist()
    {
        var dto = new WorkspaceUpdateDto { IdCenter = 99, IdProduct = 2, OptimizedQuantity = 10, Approved = true };
        _centerRepository.Exists(99, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.UpdateWorkspaceAsync(dto));

        await _workspaceRepository.DidNotReceive().GetByKeyAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateWorkspaceAsync_ThrowsBadRequestException_WhenProductDoesNotExist()
    {
        var dto = new WorkspaceUpdateDto { IdCenter = 1, IdProduct = 99, OptimizedQuantity = 10, Approved = true };
        _productRepository.Exists(99, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.UpdateWorkspaceAsync(dto));

        await _workspaceRepository.DidNotReceive().GetByKeyAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClearWorkspaceAsync_ClearsWorkspacesForCurrentUser()
    {
        await _sut.ClearWorkspaceAsync();

        await _workspaceRepository.Received(1).ClearByUserAsync(1, Arg.Any<CancellationToken>());
    }
}
