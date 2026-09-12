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
    private readonly BufferAdjustmentFactorService _sut;

    public BufferAdjustmentFactorServiceTests()
    {
        _productRepository.Exists(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        _centerRepository.Exists(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        _sut = new BufferAdjustmentFactorService(_bufferAdjustmentFactorRepository, _productRepository, _centerRepository);
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
    public async Task AddAsync_AlwaysPersistsOldFieldsAsNull()
    {
        var postDto = BuildPostDto();
        _bufferAdjustmentFactorRepository.AddAsync(Arg.Any<BufferAdjustmentFactor>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<BufferAdjustmentFactor>());

        var result = await _sut.AddAsync(postDto);

        Assert.Null(result.BufferTypeOld);
        Assert.Null(result.BufferDdmrpRedOld);
        Assert.Null(result.BufferDdmrpYellowOld);
        Assert.Null(result.BufferDdmrpGreenOld);
        await _bufferAdjustmentFactorRepository.Received(1).AddAsync(
            Arg.Is<BufferAdjustmentFactor>(b => b.BufferTypeOld == null && b.BufferDdmrpRedOld == null),
            Arg.Any<CancellationToken>());
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
    public async Task UpdateAsync_UpdatesFields_ButKeepsIdProductAndIdCenter()
    {
        var existing = new BufferAdjustmentFactor
        {
            Id = 1,
            IdProduct = 1,
            IdCenter = 1,
            BufferType = BufferType.Normal,
            BufferDdmrpRed = 10m,
            BufferDdmrpYellow = 20m,
            BufferDdmrpGreen = 30m
        };
        var putDto = new BufferAdjustmentFactorPutDto
        {
            EffectiveFrom = new DateTime(2026, 2, 1),
            EffectiveTo = new DateTime(2026, 11, 30),
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
}
