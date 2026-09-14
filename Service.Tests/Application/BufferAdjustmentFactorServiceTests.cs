using NSubstitute;
using Service.Application.DTOs.BufferAdjustmentFactor;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Entities;
using Service.Domain.Enums;
using Service.Domain.Interfaces;

namespace Service.Tests.Application;

public class BufferAdjustmentFactorServiceTests
{
    private readonly IBufferAdjustmentFactorRepository _bufferAdjustmentFactorRepository = Substitute.For<IBufferAdjustmentFactorRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ICenterRepository _centerRepository = Substitute.For<ICenterRepository>();
    private readonly ICenterProductRepository _centerProductRepository = Substitute.For<ICenterProductRepository>();
    private readonly BufferAdjustmentFactorService _sut;

    public BufferAdjustmentFactorServiceTests()
    {
        _productRepository.Exists(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        _centerRepository.Exists(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        _sut = new BufferAdjustmentFactorService(_bufferAdjustmentFactorRepository, _productRepository, _centerRepository, _centerProductRepository);
    }

    private static BufferAdjustmentFactorPostDto BuildPostDto() => new()
    {
        IdProduct = 1,
        IdCenter = 1,
        EffectiveFrom = new DateTime(2026, 1, 1),
        EffectiveTo = new DateTime(2026, 12, 31),
        BufferType = BufferType.Normal,
        BufferDdmrpRed = 10m,
        BufferDdmrpYellow = 20m,
        BufferDdmrpGreen = 30m,
        Obs = "Buffer inicial",
        IsActive = true
    };

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenBufferAdjustmentFactorExists()
    {
        var entity = new BufferAdjustmentFactor { Id = 1, IdProduct = 1, IdCenter = 1, BufferType = BufferType.MinMax };
        _bufferAdjustmentFactorRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(entity);

        var result = await _sut.GetByIdAsync(1);

        Assert.Equal(entity.Id, result.Id);
        Assert.Equal(BufferType.MinMax, result.BufferType);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFoundException_WhenBufferAdjustmentFactorDoesNotExist()
    {
        _bufferAdjustmentFactorRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((BufferAdjustmentFactor)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(1));
    }

    [Fact]
    public async Task AddAsync_LeavesOldFieldsNull_WhenNoCenterProductExists()
    {
        var postDto = BuildPostDto();
        _centerProductRepository.GetByProductAndCenterAsync(postDto.IdProduct, postDto.IdCenter, Arg.Any<CancellationToken>())
            .Returns((CenterProduct)null!);
        _bufferAdjustmentFactorRepository.AddAsync(Arg.Any<BufferAdjustmentFactor>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<BufferAdjustmentFactor>());

        var result = await _sut.AddAsync(postDto);

        Assert.Null(result.BufferTypeOld);
        Assert.Null(result.BufferDdmrpRedSafeOld);
        Assert.Null(result.BufferDdmrpRedBaseOld);
        Assert.Null(result.BufferDdmrpYellowOld);
        Assert.Null(result.BufferDdmrpGreenOld);
    }

    [Fact]
    public async Task AddAsync_SnapshotsOldFieldsFromCenterProduct_WhenCenterProductExists()
    {
        var postDto = BuildPostDto();
        var centerProduct = new CenterProduct
        {
            IdProduct = postDto.IdProduct,
            IdCenter = postDto.IdCenter,
            BufferType = BufferType.MinMax,
            RedZoneSafe = 1m,
            RedZoneBase = 2m,
            YellowZone = 3m,
            GreenZone = 4m
        };
        _centerProductRepository.GetByProductAndCenterAsync(postDto.IdProduct, postDto.IdCenter, Arg.Any<CancellationToken>())
            .Returns(centerProduct);
        _bufferAdjustmentFactorRepository.AddAsync(Arg.Any<BufferAdjustmentFactor>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<BufferAdjustmentFactor>());

        var result = await _sut.AddAsync(postDto);

        Assert.Equal(BufferType.MinMax, result.BufferTypeOld);
        Assert.Equal(1m, result.BufferDdmrpRedSafeOld);
        Assert.Equal(2m, result.BufferDdmrpRedBaseOld);
        Assert.Equal(3m, result.BufferDdmrpYellowOld);
        Assert.Equal(4m, result.BufferDdmrpGreenOld);
    }

    [Fact]
    public async Task AddAsync_ThrowsBadRequestException_WhenProductDoesNotExist()
    {
        var postDto = BuildPostDto();
        _productRepository.Exists(postDto.IdProduct, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddAsync(postDto));

        await _bufferAdjustmentFactorRepository.DidNotReceive().AddAsync(Arg.Any<BufferAdjustmentFactor>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_ThrowsBadRequestException_WhenCenterDoesNotExist()
    {
        var postDto = BuildPostDto();
        _centerRepository.Exists(postDto.IdCenter, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddAsync(postDto));
    }

    [Fact]
    public async Task AddAsync_ThrowsBadRequestException_WhenOverlappingActiveBufferAdjustmentFactorExists()
    {
        var postDto = BuildPostDto();
        _bufferAdjustmentFactorRepository.ExistsOverlappingAsync(postDto.IdProduct, postDto.IdCenter, postDto.EffectiveFrom, postDto.EffectiveTo, Arg.Any<CancellationToken>())
            .Returns(true);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddAsync(postDto));

        await _bufferAdjustmentFactorRepository.DidNotReceive().AddAsync(Arg.Any<BufferAdjustmentFactor>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFields_ButKeepsIdProductAndIdCenterAndPeriod()
    {
        var existing = new BufferAdjustmentFactor
        {
            Id = 1,
            IdProduct = 1,
            IdCenter = 1,
            BufferType = BufferType.Normal,
            BufferDdmrpRed = 10m,
            BufferDdmrpYellow = 20m,
            BufferDdmrpGreen = 30m,
            EffectiveFrom = new DateTime(2026, 1, 1),
            EffectiveTo = new DateTime(2026, 12, 31)
        };
        var putDto = new BufferAdjustmentFactorPutDto
        {
            BufferType = BufferType.DynamicMinMax,
            BufferDdmrpRed = 15m,
            BufferDdmrpYellow = 25m,
            BufferDdmrpGreen = 35m,
            IsActive = false
        };
        _bufferAdjustmentFactorRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _bufferAdjustmentFactorRepository.UpdateAsync(Arg.Any<BufferAdjustmentFactor>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<BufferAdjustmentFactor>());

        var result = await _sut.UpdateAsync(1, putDto);

        Assert.Equal(BufferType.DynamicMinMax, result.BufferType);
        Assert.Equal(15m, result.BufferDdmrpRed);
        Assert.False(result.IsActive);
        Assert.Equal(1, result.IdProduct);
        Assert.Equal(1, result.IdCenter);
        Assert.Equal(new DateTime(2026, 1, 1), result.EffectiveFrom);
        Assert.Equal(new DateTime(2026, 12, 31), result.EffectiveTo);
        Assert.Null(result.BufferTypeOld);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsNotFoundException_WhenBufferAdjustmentFactorDoesNotExist()
    {
        _bufferAdjustmentFactorRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((BufferAdjustmentFactor)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateAsync(1, new BufferAdjustmentFactorPutDto
        {
            BufferType = BufferType.Normal,
            BufferDdmrpRed = 1m,
            BufferDdmrpYellow = 1m,
            BufferDdmrpGreen = 1m
        }));
    }

    [Fact]
    public async Task DeleteAsync_ThrowsNotFoundException_WhenBufferAdjustmentFactorDoesNotExist()
    {
        _bufferAdjustmentFactorRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns((BufferAdjustmentFactor)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(1));
    }

    [Fact]
    public async Task SetActiveAsync_UpdatesOnlyIsActive_WhenBufferAdjustmentFactorExists()
    {
        var existing = new BufferAdjustmentFactor { Id = 1, IdProduct = 1, IdCenter = 1, BufferDdmrpRed = 10m, IsActive = true };
        _bufferAdjustmentFactorRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _bufferAdjustmentFactorRepository.UpdateAsync(Arg.Any<BufferAdjustmentFactor>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<BufferAdjustmentFactor>());

        var result = await _sut.SetActiveAsync(1, false);

        Assert.False(result.IsActive);
        Assert.Equal(10m, result.BufferDdmrpRed);
    }

    [Fact]
    public async Task SetActiveAsync_ThrowsNotFoundException_WhenBufferAdjustmentFactorDoesNotExist()
    {
        _bufferAdjustmentFactorRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((BufferAdjustmentFactor)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.SetActiveAsync(1, true));
    }

    private static BufferAdjustmentFactor BuildActiveInPeriodEntity() => new()
    {
        Id = 1,
        IdProduct = 1,
        IdCenter = 1,
        IsActive = true,
        EffectiveFrom = DateTime.Now.AddDays(-1),
        EffectiveTo = DateTime.Now.AddDays(1),
        BufferTypeOld = BufferType.Normal,
        BufferDdmrpRedSafeOld = 1m,
        BufferDdmrpRedBaseOld = 2m,
        BufferDdmrpYellowOld = 3m,
        BufferDdmrpGreenOld = 4m,
        AlreadyReverted = false
    };

    [Fact]
    public async Task SetActiveAsync_RevertsCenterProductAndSetsAlreadyReverted_WhenDeactivatingAnActiveInPeriodBaf()
    {
        var existing = BuildActiveInPeriodEntity();
        var centerProduct = new CenterProduct { IdProduct = 1, IdCenter = 1, BufferType = BufferType.ManualFixed };
        _bufferAdjustmentFactorRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _bufferAdjustmentFactorRepository.UpdateAsync(Arg.Any<BufferAdjustmentFactor>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<BufferAdjustmentFactor>());
        _centerProductRepository.GetByProductAndCenterAsync(1, 1, Arg.Any<CancellationToken>()).Returns(centerProduct);

        var result = await _sut.SetActiveAsync(1, false);

        Assert.False(result.IsActive);
        Assert.True(result.AlreadyReverted);
        Assert.Equal(BufferType.Normal, centerProduct.BufferType);
        Assert.Equal(1m, centerProduct.RedZoneSafe);
        Assert.Equal(2m, centerProduct.RedZoneBase);
        Assert.Equal(3m, centerProduct.YellowZone);
        Assert.Equal(4m, centerProduct.GreenZone);
        await _centerProductRepository.Received(1).UpdateAsync(centerProduct, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetActiveAsync_DoesNotRevert_WhenDeactivatingABafOutsideItsPeriod()
    {
        var existing = BuildActiveInPeriodEntity();
        existing.EffectiveTo = DateTime.Now.AddDays(-1);
        _bufferAdjustmentFactorRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _bufferAdjustmentFactorRepository.UpdateAsync(Arg.Any<BufferAdjustmentFactor>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<BufferAdjustmentFactor>());

        var result = await _sut.SetActiveAsync(1, false);

        Assert.False(result.IsActive);
        Assert.False(result.AlreadyReverted);
        await _centerProductRepository.DidNotReceive().GetByProductAndCenterAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetActiveAsync_ResetsAlreadyReverted_WhenReactivating()
    {
        var existing = BuildActiveInPeriodEntity();
        existing.IsActive = false;
        existing.AlreadyReverted = true;
        _bufferAdjustmentFactorRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _bufferAdjustmentFactorRepository.UpdateAsync(Arg.Any<BufferAdjustmentFactor>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<BufferAdjustmentFactor>());

        var result = await _sut.SetActiveAsync(1, true);

        Assert.True(result.IsActive);
        Assert.False(result.AlreadyReverted);
    }

    [Fact]
    public async Task DeleteAsync_RevertsCenterProductAndSetsAlreadyReverted_WhenDeletingAnActiveInPeriodBaf()
    {
        var existing = BuildActiveInPeriodEntity();
        var centerProduct = new CenterProduct { IdProduct = 1, IdCenter = 1, BufferType = BufferType.ManualFixed };
        _bufferAdjustmentFactorRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _bufferAdjustmentFactorRepository.DeleteAsync(1, Arg.Any<CancellationToken>())
            .Returns(callInfo => existing);
        _centerProductRepository.GetByProductAndCenterAsync(1, 1, Arg.Any<CancellationToken>()).Returns(centerProduct);

        var result = await _sut.DeleteAsync(1);

        Assert.True(result.AlreadyReverted);
        Assert.Equal(BufferType.Normal, centerProduct.BufferType);
        await _centerProductRepository.Received(1).UpdateAsync(centerProduct, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_DoesNotRevert_WhenBafIsNotActive()
    {
        var existing = BuildActiveInPeriodEntity();
        existing.IsActive = false;
        _bufferAdjustmentFactorRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _bufferAdjustmentFactorRepository.DeleteAsync(1, Arg.Any<CancellationToken>())
            .Returns(callInfo => existing);

        var result = await _sut.DeleteAsync(1);

        Assert.False(result.AlreadyReverted);
        await _centerProductRepository.DidNotReceive().GetByProductAndCenterAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}
