using NSubstitute;
using Service.Application.DTOs.DemandAdjustmentFactor;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Entities;
using Service.Domain.Enums;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;

namespace Service.Tests.Application;

public class DemandAdjustmentFactorServiceTests
{
    private readonly IDemandAdjustmentFactorRepository _demandAdjustmentFactorRepository = Substitute.For<IDemandAdjustmentFactorRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ICenterRepository _centerRepository = Substitute.For<ICenterRepository>();
    private readonly DemandAdjustmentFactorService _sut;

    public DemandAdjustmentFactorServiceTests()
    {
        _productRepository.Exists(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        _centerRepository.Exists(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        _sut = new DemandAdjustmentFactorService(_demandAdjustmentFactorRepository, _productRepository, _centerRepository);
    }

    private static DemandAdjustmentFactorPostDto BuildPostDto() => new()
    {
        IdProduct = 1,
        IdCenter = 1,
        EffectiveFrom = new DateTime(2026, 1, 1),
        EffectiveTo = new DateTime(2026, 12, 31),
        IsActive = true,
        Obs = "Ajuste de demanda",
        AdjustmentType = AdjustmentType.Percentage,
        AdjustmentValue = 10m
    };

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenDemandAdjustmentFactorExists()
    {
        var entity = new DemandAdjustmentFactor { Id = 1, IdProduct = 1, IdCenter = 1, AdjustmentType = AdjustmentType.FlatValue, AdjustmentValue = 5m };
        _demandAdjustmentFactorRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(entity);

        var result = await _sut.GetByIdAsync(1);

        Assert.Equal(entity.Id, result.Id);
        Assert.Equal(AdjustmentType.FlatValue, result.AdjustmentType);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFoundException_WhenDemandAdjustmentFactorDoesNotExist()
    {
        _demandAdjustmentFactorRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((DemandAdjustmentFactor)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(1));
    }

    [Fact]
    public async Task AddAsync_PersistsMappedEntity_WhenForeignKeysExist()
    {
        var postDto = BuildPostDto();
        _demandAdjustmentFactorRepository.AddAsync(Arg.Any<DemandAdjustmentFactor>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<DemandAdjustmentFactor>());

        var result = await _sut.AddAsync(postDto);

        Assert.Equal(AdjustmentType.Percentage, result.AdjustmentType);
        Assert.Equal(10m, result.AdjustmentValue);
        await _demandAdjustmentFactorRepository.Received(1).AddAsync(
            Arg.Is<DemandAdjustmentFactor>(d => d.IdProduct == postDto.IdProduct && d.IdCenter == postDto.IdCenter),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_ThrowsBadRequestException_WhenProductDoesNotExist()
    {
        var postDto = BuildPostDto();
        _productRepository.Exists(postDto.IdProduct, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddAsync(postDto));

        await _demandAdjustmentFactorRepository.DidNotReceive().AddAsync(Arg.Any<DemandAdjustmentFactor>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_ThrowsBadRequestException_WhenCenterDoesNotExist()
    {
        var postDto = BuildPostDto();
        _centerRepository.Exists(postDto.IdCenter, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddAsync(postDto));
    }

    [Fact]
    public async Task AddAsync_ThrowsBadRequestException_WhenOverlappingActiveDemandAdjustmentFactorExists()
    {
        var postDto = BuildPostDto();
        _demandAdjustmentFactorRepository.ExistsOverlappingAsync(postDto.IdProduct, postDto.IdCenter, postDto.EffectiveFrom, postDto.EffectiveTo, Arg.Any<CancellationToken>())
            .Returns(true);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddAsync(postDto));

        await _demandAdjustmentFactorRepository.DidNotReceive().AddAsync(Arg.Any<DemandAdjustmentFactor>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_AppliesChanges_ButKeepsIdProductAndIdCenterAndPeriod()
    {
        var existing = new DemandAdjustmentFactor
        {
            Id = 1,
            IdProduct = 1,
            IdCenter = 1,
            AdjustmentType = AdjustmentType.Percentage,
            AdjustmentValue = 10m,
            EffectiveFrom = new DateTime(2026, 1, 1),
            EffectiveTo = new DateTime(2026, 12, 31)
        };
        var putDto = new DemandAdjustmentFactorPutDto
        {
            IsActive = false,
            AdjustmentType = AdjustmentType.FlatValue,
            AdjustmentValue = 20m
        };
        _demandAdjustmentFactorRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _demandAdjustmentFactorRepository.UpdateAsync(Arg.Any<DemandAdjustmentFactor>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<DemandAdjustmentFactor>());

        var result = await _sut.UpdateAsync(1, putDto);

        Assert.Equal(AdjustmentType.FlatValue, result.AdjustmentType);
        Assert.Equal(20m, result.AdjustmentValue);
        Assert.False(result.IsActive);
        Assert.Equal(1, result.IdProduct);
        Assert.Equal(1, result.IdCenter);
        Assert.Equal(new DateTime(2026, 1, 1), result.EffectiveFrom);
        Assert.Equal(new DateTime(2026, 12, 31), result.EffectiveTo);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsNotFoundException_WhenDemandAdjustmentFactorDoesNotExist()
    {
        _demandAdjustmentFactorRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns((DemandAdjustmentFactor)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(1));
    }

    [Fact]
    public async Task SetActiveAsync_UpdatesOnlyIsActive_WhenDemandAdjustmentFactorExists()
    {
        var existing = new DemandAdjustmentFactor { Id = 1, IdProduct = 1, IdCenter = 1, AdjustmentValue = 10m, IsActive = true };
        _demandAdjustmentFactorRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _demandAdjustmentFactorRepository.UpdateAsync(Arg.Any<DemandAdjustmentFactor>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<DemandAdjustmentFactor>());

        var result = await _sut.SetActiveAsync(1, false);

        Assert.False(result.IsActive);
        Assert.Equal(10m, result.AdjustmentValue);
    }

    [Fact]
    public async Task SetActiveAsync_ThrowsNotFoundException_WhenDemandAdjustmentFactorDoesNotExist()
    {
        _demandAdjustmentFactorRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((DemandAdjustmentFactor)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.SetActiveAsync(1, true));
    }

    [Fact]
    public async Task GetFilteredAsync_ReturnsMappedPagedList()
    {
        var entities = new List<DemandAdjustmentFactor>
        {
            new() { Id = 1, IdProduct = 2, IdCenter = 3, AdjustmentValue = 10m }
        };
        _demandAdjustmentFactorRepository.GetFilteredAsync(2, 3, 1, 10, Arg.Any<CancellationToken>())
            .Returns(new PagedList<DemandAdjustmentFactor>(entities, 1, 10, 1));

        var result = await _sut.GetFilteredAsync(idProduct: 2, idCenter: 3, pageNumber: 1, pageSize: 10);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(2, Assert.Single(result).IdProduct);
    }
}
