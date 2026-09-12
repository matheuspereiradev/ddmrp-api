using NSubstitute;
using Service.Application.DTOs.MasterBuffer;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Tests.Application;

public class MasterBufferServiceTests
{
    private readonly IMasterBufferRepository _masterBufferRepository = Substitute.For<IMasterBufferRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ICenterRepository _centerRepository = Substitute.For<ICenterRepository>();
    private readonly MasterBufferService _sut;

    public MasterBufferServiceTests()
    {
        _productRepository.Exists(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        _centerRepository.Exists(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        _sut = new MasterBufferService(_masterBufferRepository, _productRepository, _centerRepository);
    }

    private static MasterBufferPostDto BuildPostDto() => new()
    {
        IdProduct = 1,
        IdCenter = 1,
        IdProductFather = 2,
        IdCenterFather = 2,
        Sequency = 1
    };

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenMasterBufferExists()
    {
        var masterBuffer = new MasterBuffer { Id = 1, IdProduct = 1, IdCenter = 1, IdProductFather = 2, IdCenterFather = 2, Sequency = 1 };
        _masterBufferRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(masterBuffer);

        var result = await _sut.GetByIdAsync(1);

        Assert.Equal(masterBuffer.Id, result.Id);
        Assert.Equal(masterBuffer.IdProductFather, result.IdProductFather);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFoundException_WhenMasterBufferDoesNotExist()
    {
        _masterBufferRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((MasterBuffer)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(1));
    }

    [Fact]
    public async Task GetByIdAsync_IncludesAllRelations_WhenLoaded()
    {
        var masterBuffer = new MasterBuffer
        {
            Id = 1,
            IdProduct = 1,
            IdCenter = 1,
            IdProductFather = 2,
            IdCenterFather = 2,
            Product = new Product { Id = 1, Reference = "REF001", Description = "Produto", UnitOfMeasure = "UN" },
            Center = new Center { Id = 1, Code = "C001", Description = "Centro" },
            ProductFather = new Product { Id = 2, Reference = "REF002", Description = "Produto Pai", UnitOfMeasure = "UN" },
            CenterFather = new Center { Id = 2, Code = "C002", Description = "Centro Pai" }
        };
        _masterBufferRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(masterBuffer);

        var result = await _sut.GetByIdAsync(1);

        Assert.Equal("REF001", result.Product!.Reference);
        Assert.Equal("C001", result.Center!.Code);
        Assert.Equal("REF002", result.ProductFather!.Reference);
        Assert.Equal("C002", result.CenterFather!.Code);
    }

    [Fact]
    public async Task AddAsync_PersistsMappedEntity_WhenAllForeignKeysExist()
    {
        var postDto = BuildPostDto();
        _masterBufferRepository.AddAsync(Arg.Any<MasterBuffer>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<MasterBuffer>());

        var result = await _sut.AddAsync(postDto);

        Assert.Equal(postDto.IdProduct, result.IdProduct);
        Assert.Equal(postDto.IdProductFather, result.IdProductFather);
        await _masterBufferRepository.Received(1).AddAsync(
            Arg.Is<MasterBuffer>(mb => mb.IdProduct == postDto.IdProduct && mb.IdProductFather == postDto.IdProductFather),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_ThrowsBadRequestException_WhenProductDoesNotExist()
    {
        var postDto = BuildPostDto();
        _productRepository.Exists(postDto.IdProduct, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddAsync(postDto));

        await _masterBufferRepository.DidNotReceive().AddAsync(Arg.Any<MasterBuffer>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_ThrowsBadRequestException_WhenCenterDoesNotExist()
    {
        var postDto = BuildPostDto();
        _centerRepository.Exists(postDto.IdCenter, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddAsync(postDto));
    }

    [Fact]
    public async Task AddAsync_ThrowsBadRequestException_WhenFatherProductDoesNotExist()
    {
        var postDto = BuildPostDto();
        _productRepository.Exists(postDto.IdProductFather, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddAsync(postDto));
    }

    [Fact]
    public async Task AddAsync_ThrowsBadRequestException_WhenFatherCenterDoesNotExist()
    {
        var postDto = BuildPostDto();
        _centerRepository.Exists(postDto.IdCenterFather, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddAsync(postDto));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOnlySequency_WhenMasterBufferExists()
    {
        var existing = new MasterBuffer { Id = 1, IdProduct = 1, IdCenter = 1, IdProductFather = 2, IdCenterFather = 2, Sequency = 1 };
        var putDto = new MasterBufferPutDto { Sequency = 5 };
        _masterBufferRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _masterBufferRepository.UpdateAsync(Arg.Any<MasterBuffer>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<MasterBuffer>());

        var result = await _sut.UpdateAsync(1, putDto);

        Assert.Equal(5, result.Sequency);
        Assert.Equal(1, result.IdProduct);
        Assert.Equal(2, result.IdProductFather);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsNotFoundException_WhenMasterBufferDoesNotExist()
    {
        _masterBufferRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((MasterBuffer)null!);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UpdateAsync(1, new MasterBufferPutDto { Sequency = 1 }));
    }

    [Fact]
    public async Task DeleteAsync_ThrowsNotFoundException_WhenMasterBufferDoesNotExist()
    {
        _masterBufferRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns((MasterBuffer)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(1));
    }
}
