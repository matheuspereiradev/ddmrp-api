using NSubstitute;
using Service.Application.DTOs.AllocationGroup;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Tests.Application;

public class AllocationGroupServiceTests
{
    private readonly IAllocationGroupRepository _allocationGroupRepository = Substitute.For<IAllocationGroupRepository>();
    private readonly AllocationGroupService _sut;

    public AllocationGroupServiceTests()
    {
        _sut = new AllocationGroupService(_allocationGroupRepository);
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
