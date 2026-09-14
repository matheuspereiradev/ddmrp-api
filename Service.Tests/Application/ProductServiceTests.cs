using NSubstitute;
using Service.Application.DTOs.Product;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;

namespace Service.Tests.Application;

public class ProductServiceTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ICenterRepository _centerRepository = Substitute.For<ICenterRepository>();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _centerRepository.Exists(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        _sut = new ProductService(_productRepository, _centerRepository);
    }

    private static ProductPostDto BuildPostDto() => new()
    {
        Reference = "REF001",
        Description = "Produto Teste",
        AuxiliarMaterialCode = "AUX001",
        UnitOfMeasure = "UN",
        Weight = 1.5m,
        Volume = 0.02m,
        Barcode = "7891234567890",
        Category = "Categoria A",
        Segment = "Segmento A",
        Value = 99.90m,
        Pallet = 100m,
        Line = "Linha A",
        Subline = "Sublinha A",
        Brand = "Marca A",
        WorkCenter = "WC01"
    };

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenProductExists()
    {
        var product = new Product { Id = 1, Reference = "REF001", Description = "Produto Teste" };
        _productRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _sut.GetByIdAsync(1);

        Assert.Equal(product.Id, result.Id);
        Assert.Equal(product.Reference, result.Reference);
        Assert.Equal(product.Description, result.Description);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFoundException_WhenProductDoesNotExist()
    {
        _productRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Product)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(1));
    }

    [Fact]
    public async Task AddAsync_PersistsMappedEntity()
    {
        var postDto = BuildPostDto();
        _productRepository.AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Product>());

        var result = await _sut.AddAsync(postDto);

        Assert.Equal("REF001", result.Reference);
        Assert.Equal(1.5m, result.Weight);
        Assert.Equal(99.90m, result.Value);
        await _productRepository.Received(1).AddAsync(
            Arg.Is<Product>(p => p.Reference == "REF001" && p.Barcode == "7891234567890"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_AppliesChanges_WhenProductExists()
    {
        var existing = new Product { Id = 1, Reference = "REF001", Description = "Old" };
        var putDto = BuildPostDtoAsPut();
        _productRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _productRepository.UpdateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Product>());

        var result = await _sut.UpdateAsync(1, putDto);

        Assert.Equal("Produto Teste", result.Description);
        Assert.Equal(99.90m, result.Value);
    }

    private static ProductPutDto BuildPostDtoAsPut() => new()
    {
        Reference = "REF001",
        Description = "Produto Teste",
        AuxiliarMaterialCode = "AUX001",
        UnitOfMeasure = "UN",
        Weight = 1.5m,
        Volume = 0.02m,
        Barcode = "7891234567890",
        Category = "Categoria A",
        Segment = "Segmento A",
        Value = 99.90m,
        Pallet = 100m,
        Line = "Linha A",
        Subline = "Sublinha A",
        Brand = "Marca A",
        WorkCenter = "WC01"
    };

    [Fact]
    public async Task DeleteAsync_ThrowsNotFoundException_WhenProductDoesNotExist()
    {
        _productRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns((Product)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(1));
    }

    [Fact]
    public async Task GetByCenterAsync_ReturnsMappedPagedList_WhenCenterExists()
    {
        var products = new List<Product>
        {
            new() { Id = 1, Reference = "REF001", Description = "Produto 1" },
            new() { Id = 2, Reference = "REF002", Description = "Produto 2" }
        };
        _productRepository.GetByCenterAsync(5, 1, 10, Arg.Any<CancellationToken>())
            .Returns(new PagedList<Product>(products, 1, 10, 2));

        var result = await _sut.GetByCenterAsync(5, 1, 10);

        Assert.Equal(2, result.Count);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal("REF001", result[0].Reference);
    }

    [Fact]
    public async Task GetByCenterAsync_ThrowsNotFoundException_WhenCenterDoesNotExist()
    {
        _centerRepository.Exists(5, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByCenterAsync(5, 1, 10));

        await _productRepository.DidNotReceive().GetByCenterAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}
