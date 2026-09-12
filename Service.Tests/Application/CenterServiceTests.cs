using NSubstitute;
using Service.Application.DTOs.Center;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Tests.Application;

public class CenterServiceTests
{
    private readonly ICenterRepository _centerRepository = Substitute.For<ICenterRepository>();
    private readonly CenterService _sut;

    public CenterServiceTests()
    {
        _sut = new CenterService(_centerRepository);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenCenterExists()
    {
        var center = new Center { Id = 1, Code = "SP01", Description = "São Paulo", City = "São Paulo", Zone = "Sudeste" };
        _centerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(center);

        var result = await _sut.GetByIdAsync(1);

        Assert.Equal(center.Id, result.Id);
        Assert.Equal(center.Code, result.Code);
        Assert.Equal(center.Description, result.Description);
        Assert.Equal(center.City, result.City);
        Assert.Equal(center.Zone, result.Zone);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFoundException_WhenCenterDoesNotExist()
    {
        _centerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Center)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(1));
    }

    [Fact]
    public async Task AddAsync_PersistsMappedEntity()
    {
        var postDto = new CenterPostDto { Code = "SP01", Description = "São Paulo", City = "São Paulo", Zone = "Sudeste" };
        _centerRepository.AddAsync(Arg.Any<Center>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Center>());

        var result = await _sut.AddAsync(postDto);

        Assert.Equal("SP01", result.Code);
        await _centerRepository.Received(1).AddAsync(
            Arg.Is<Center>(c => c.Code == "SP01" && c.City == "São Paulo"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_AppliesChanges_WhenCenterExists()
    {
        var existing = new Center { Id = 1, Code = "SP01", Description = "Old", City = "São Paulo", Zone = "Sudeste" };
        var putDto = new CenterPutDto { Code = "SP02", Description = "New", City = "Campinas", Zone = "Sudeste" };
        _centerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _centerRepository.UpdateAsync(Arg.Any<Center>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Center>());

        var result = await _sut.UpdateAsync(1, putDto);

        Assert.Equal("SP02", result.Code);
        Assert.Equal("New", result.Description);
        Assert.Equal("Campinas", result.City);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsNotFoundException_WhenCenterDoesNotExist()
    {
        _centerRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns((Center)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(1));
    }
}
