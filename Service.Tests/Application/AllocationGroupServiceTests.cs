using NSubstitute;
using Service.Application.DTOs.AllocationGroup;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Account;
using Service.Domain.AllocationGroups;
using Service.Domain.Entities;
using Service.Domain.Enums;
using Service.Domain.Interfaces;
using Service.Domain.Report.Results;

namespace Service.Tests.Application;

public class AllocationGroupServiceTests
{
    private readonly IAllocationGroupRepository _allocationGroupRepository = Substitute.For<IAllocationGroupRepository>();
    private readonly IReportRepository _reportRepository = Substitute.For<IReportRepository>();
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly AllocationGroupService _sut;

    public AllocationGroupServiceTests()
    {
        _currentUser.UserId.Returns(1);
        _sut = new AllocationGroupService(_allocationGroupRepository, _reportRepository, _workspaceRepository, _currentUser);
    }

    [Fact]
    public async Task GetPriorizedAllocationAsync_ReturnsRepositoryResult_ForCurrentUser()
    {
        var rows = new List<PriorizedAllocationRow>
        {
            new() { Id = 1, Name = "Grupo A", ApprovedQuantityUnit = 10 }
        };
        _allocationGroupRepository.GetPriorizedAllocationAsync(1, Arg.Any<CancellationToken>()).Returns(rows);

        var result = await _sut.GetPriorizedAllocationAsync();

        Assert.Same(rows, result);
    }

    [Fact]
    public async Task RunPriorizedAllocationAsync_ThrowsNotFoundException_WhenGroupDoesNotExist()
    {
        _allocationGroupRepository.Exists(1, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.RunPriorizedAllocationAsync(new PriorizedAllocationRunDto { IdGroup = 1, Limit = 10, StopCondition = PriorizedAllocationStopCondition.Zero }));
    }

    [Fact]
    public async Task RunPriorizedAllocationAsync_UpdatesWorkspaceQuantities_AndReturnsItems()
    {
        _allocationGroupRepository.Exists(1, Arg.Any<CancellationToken>()).Returns(true);
        _reportRepository.GetApprovedByAllocationGroupAsync(1, Arg.Any<CancellationToken>()).Returns(new List<InventoryBufferManagementRow>
        {
            new() { Id = 10, IdCenter = 1, IdProduct = 1, OptimizedOrderQuantity = 4, Netflow = 0, TopOfGreen = 20, Moq = 0, PackQuantity = 2 }
        });
        var workspace = new Workspace { Id = 99, IdCenter = 1, IdProduct = 1, IdUser = 1, OptimizedQuantity = 4, Approved = true };
        _workspaceRepository.GetByKeyAsync(1, 1, 1, Arg.Any<CancellationToken>()).Returns(workspace);

        var result = await _sut.RunPriorizedAllocationAsync(new PriorizedAllocationRunDto { IdGroup = 1, Limit = 6, StopCondition = PriorizedAllocationStopCondition.Zero });

        var item = Assert.Single(result);
        Assert.Equal(6, item.ApprovedQuantity);
        await _workspaceRepository.Received(1).UpdateAsync(
            Arg.Is<Workspace>(w => w.OptimizedQuantity == 6),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenAllocationGroupExists()
    {
        var allocationGroup = new AllocationGroup { Id = 1, Name = "Grupo A" };
        _allocationGroupRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(allocationGroup);

        var result = await _sut.GetByIdAsync(1);

        Assert.Equal(allocationGroup.Id, result.Id);
        Assert.Equal(allocationGroup.Name, result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFoundException_WhenAllocationGroupDoesNotExist()
    {
        _allocationGroupRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((AllocationGroup)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(1));
    }

    [Fact]
    public async Task AddAsync_PersistsMappedEntity()
    {
        var postDto = new AllocationGroupPostDto { Name = "Grupo A" };
        _allocationGroupRepository.AddAsync(Arg.Any<AllocationGroup>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<AllocationGroup>());

        var result = await _sut.AddAsync(postDto);

        Assert.Equal("Grupo A", result.Name);
        await _allocationGroupRepository.Received(1).AddAsync(
            Arg.Is<AllocationGroup>(a => a.Name == "Grupo A"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_AppliesChanges_WhenAllocationGroupExists()
    {
        var existing = new AllocationGroup { Id = 1, Name = "Old" };
        var putDto = new AllocationGroupPutDto { Name = "New" };
        _allocationGroupRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _allocationGroupRepository.UpdateAsync(Arg.Any<AllocationGroup>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<AllocationGroup>());

        var result = await _sut.UpdateAsync(1, putDto);

        Assert.Equal("New", result.Name);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsNotFoundException_WhenAllocationGroupDoesNotExist()
    {
        _allocationGroupRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((AllocationGroup)null!);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UpdateAsync(1, new AllocationGroupPutDto { Name = "X" }));
    }

    [Fact]
    public async Task DeleteAsync_ThrowsNotFoundException_WhenAllocationGroupDoesNotExist()
    {
        _allocationGroupRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns((AllocationGroup)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(1));
    }
}
