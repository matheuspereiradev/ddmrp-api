using NSubstitute;
using Service.Application.DTOs.ZoneAdjustmentFactor;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Entities;
using Service.Domain.Enums;
using Service.Domain.Interfaces;

namespace Service.Tests.Application;

public class ZoneAdjustmentFactorServiceTests
{
    private readonly IZoneAdjustmentFactorRepository _zoneAdjustmentFactorRepository = Substitute.For<IZoneAdjustmentFactorRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ICenterRepository _centerRepository = Substitute.For<ICenterRepository>();
    private readonly ZoneAdjustmentFactorService _sut;

    public ZoneAdjustmentFactorServiceTests()
    {
        _productRepository.Exists(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        _centerRepository.Exists(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        _sut = new ZoneAdjustmentFactorService(_zoneAdjustmentFactorRepository, _productRepository, _centerRepository);
    }

    private static ZoneAdjustmentFactorPostDto BuildPostDto() => new()
    {
        IdProduct = 1,
        IdCenter = 1,
        TargetZone = TargetZone.RedZone,
        AdjustmentType = AdjustmentType.Percentage,
        AdjustmentValue = 10m,
        Obs = "Ajuste sazonal",
        IsActive = true,
        EffectiveFrom = new DateTime(2026, 1, 1),
        EffectiveTo = new DateTime(2026, 12, 31)
    };

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenZoneAdjustmentFactorExists()
    {
        var entity = new ZoneAdjustmentFactor
        {
            Id = 1,
            IdProduct = 1,
            IdCenter = 1,
            TargetZone = TargetZone.GreenZone,
            AdjustmentType = AdjustmentType.FlatValue,
            AdjustmentValue = 5m
        };
        _zoneAdjustmentFactorRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(entity);

        var result = await _sut.GetByIdAsync(1);

        Assert.Equal(entity.Id, result.Id);
        Assert.Equal(TargetZone.GreenZone, result.TargetZone);
        Assert.Equal(AdjustmentType.FlatValue, result.AdjustmentType);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFoundException_WhenZoneAdjustmentFactorDoesNotExist()
    {
        _zoneAdjustmentFactorRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((ZoneAdjustmentFactor)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(1));
    }

    [Fact]
    public async Task AddAsync_PersistsMappedEntity_WhenForeignKeysExist()
    {
        var postDto = BuildPostDto();
        _zoneAdjustmentFactorRepository.AddAsync(Arg.Any<ZoneAdjustmentFactor>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<ZoneAdjustmentFactor>());

        var result = await _sut.AddAsync(postDto);

        Assert.Equal(TargetZone.RedZone, result.TargetZone);
        Assert.Equal(AdjustmentType.Percentage, result.AdjustmentType);
        Assert.Equal(10m, result.AdjustmentValue);
        await _zoneAdjustmentFactorRepository.Received(1).AddAsync(
            Arg.Is<ZoneAdjustmentFactor>(z => z.TargetZone == TargetZone.RedZone && z.AdjustmentType == AdjustmentType.Percentage),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_ThrowsBadRequestException_WhenProductDoesNotExist()
    {
        var postDto = BuildPostDto();
        _productRepository.Exists(postDto.IdProduct, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddAsync(postDto));

        await _zoneAdjustmentFactorRepository.DidNotReceive().AddAsync(Arg.Any<ZoneAdjustmentFactor>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_ThrowsBadRequestException_WhenCenterDoesNotExist()
    {
        var postDto = BuildPostDto();
        _centerRepository.Exists(postDto.IdCenter, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddAsync(postDto));
    }

    [Fact]
    public async Task UpdateAsync_AppliesChanges_ButKeepsIdProductAndIdCenter_WhenZoneAdjustmentFactorExists()
    {
        var existing = new ZoneAdjustmentFactor { Id = 1, IdProduct = 1, IdCenter = 1, TargetZone = TargetZone.RedZone, AdjustmentType = AdjustmentType.Percentage, AdjustmentValue = 10m };
        var putDto = new ZoneAdjustmentFactorPutDto
        {
            TargetZone = TargetZone.YellowZone,
            AdjustmentType = AdjustmentType.FlatValue,
            AdjustmentValue = 20m,
            IsActive = false,
            EffectiveFrom = new DateTime(2026, 2, 1),
            EffectiveTo = new DateTime(2026, 11, 30)
        };
        _zoneAdjustmentFactorRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _zoneAdjustmentFactorRepository.UpdateAsync(Arg.Any<ZoneAdjustmentFactor>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<ZoneAdjustmentFactor>());

        var result = await _sut.UpdateAsync(1, putDto);

        Assert.Equal(TargetZone.YellowZone, result.TargetZone);
        Assert.Equal(AdjustmentType.FlatValue, result.AdjustmentType);
        Assert.Equal(20m, result.AdjustmentValue);
        Assert.False(result.IsActive);
        Assert.Equal(1, result.IdProduct);
        Assert.Equal(1, result.IdCenter);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsNotFoundException_WhenZoneAdjustmentFactorDoesNotExist()
    {
        _zoneAdjustmentFactorRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns((ZoneAdjustmentFactor)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(1));
    }

    [Fact]
    public async Task SetActiveAsync_UpdatesOnlyIsActive_WhenZoneAdjustmentFactorExists()
    {
        var existing = new ZoneAdjustmentFactor { Id = 1, IdProduct = 1, IdCenter = 1, AdjustmentValue = 10m, IsActive = true };
        _zoneAdjustmentFactorRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _zoneAdjustmentFactorRepository.UpdateAsync(Arg.Any<ZoneAdjustmentFactor>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<ZoneAdjustmentFactor>());

        var result = await _sut.SetActiveAsync(1, false);

        Assert.False(result.IsActive);
        Assert.Equal(10m, result.AdjustmentValue);
    }

    [Fact]
    public async Task SetActiveAsync_ThrowsNotFoundException_WhenZoneAdjustmentFactorDoesNotExist()
    {
        _zoneAdjustmentFactorRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((ZoneAdjustmentFactor)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.SetActiveAsync(1, true));
    }
}
