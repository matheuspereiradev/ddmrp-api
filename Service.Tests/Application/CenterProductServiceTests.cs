using NSubstitute;
using Service.Application.DTOs.CenterProduct;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Tests.Application;

public class CenterProductServiceTests
{
    private readonly ICenterProductRepository _centerProductRepository = Substitute.For<ICenterProductRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ICenterRepository _centerRepository = Substitute.For<ICenterRepository>();
    private readonly IPartnerRepository _partnerRepository = Substitute.For<IPartnerRepository>();
    private readonly ITagRepository _tagRepository = Substitute.For<ITagRepository>();
    private readonly IReasonRepository _reasonRepository = Substitute.For<IReasonRepository>();
    private readonly IAllocationGroupRepository _allocationGroupRepository = Substitute.For<IAllocationGroupRepository>();
    private readonly IBufferProfileRepository _bufferProfileRepository = Substitute.For<IBufferProfileRepository>();
    private readonly CenterProductService _sut;

    public CenterProductServiceTests()
    {
        _productRepository.Exists(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        _centerRepository.Exists(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        _partnerRepository.Exists(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        _tagRepository.Exists(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        _reasonRepository.Exists(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        _allocationGroupRepository.Exists(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        _bufferProfileRepository.Exists(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);

        _sut = new CenterProductService(
            _centerProductRepository, _productRepository, _centerRepository,
            _partnerRepository, _tagRepository, _reasonRepository, _allocationGroupRepository, _bufferProfileRepository);
    }

    private static CenterProductPostDto BuildPostDto() => new()
    {
        IdProduct = 1,
        IdCenter = 1,
        IdOriginCenter = 2,
        PackQuantity = 10m,
        Moq = 5m,
        LeadTime = 7,
        Frequency = 30,
        Class = "A",
        Classification = "Alta",
        Segment = "Varejo",
        Stock = 100m,
        IdProvider = 1,
        IdTag = 1,
        IdReason = 1,
        IdAllocationGroup = 1,
        IdBufferProfile = 1
    };

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenCenterProductExists()
    {
        var centerProduct = new CenterProduct { Id = 1, IdProduct = 1, IdCenter = 1, PackQuantity = 10m, Moq = 5m, Stock = 100m };
        _centerProductRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(centerProduct);

        var result = await _sut.GetByIdAsync(1);

        Assert.Equal(centerProduct.Id, result.Id);
        Assert.Equal(centerProduct.IdProduct, result.IdProduct);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFoundException_WhenCenterProductDoesNotExist()
    {
        _centerProductRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((CenterProduct)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(1));
    }

    [Fact]
    public async Task GetByIdAsync_IncludesAllRelations_WhenLoaded()
    {
        var centerProduct = new CenterProduct
        {
            Id = 1,
            IdProduct = 1,
            IdCenter = 2,
            Product = new Product { Id = 1, Reference = "REF001", Description = "Produto", UnitOfMeasure = "UN" },
            Center = new Center { Id = 2, Code = "C001", Description = "Centro" },
            OriginCenter = new Center { Id = 3, Code = "C002", Description = "Centro Origem" },
            Provider = new Partner { Id = 1, Code = "P001", Description = "Fornecedor" },
            Tag = new Tag { Id = 1, Name = "Promoção" },
            Reason = new Reason { Id = 1, Name = "Avaria" },
            AllocationGroup = new AllocationGroup { Id = 1, Name = "Grupo A" },
            BufferProfile = new BufferProfile { Id = 1, ProfileName = "P1" }
        };
        _centerProductRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(centerProduct);

        var result = await _sut.GetByIdAsync(1);

        Assert.Equal("REF001", result.Product!.Reference);
        Assert.Equal("C001", result.Center!.Code);
        Assert.Equal("C002", result.OriginCenter!.Code);
        Assert.Equal("P001", result.Provider!.Code);
        Assert.Equal("Promoção", result.Tag!.Name);
        Assert.Equal("Avaria", result.Reason!.Name);
        Assert.Equal("Grupo A", result.AllocationGroup!.Name);
        Assert.Equal("P1", result.BufferProfile!.ProfileName);
    }

    [Fact]
    public async Task AddAsync_PersistsMappedEntity_WhenAllForeignKeysExist()
    {
        var postDto = BuildPostDto();
        _centerProductRepository.AddAsync(Arg.Any<CenterProduct>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<CenterProduct>());

        var result = await _sut.AddAsync(postDto);

        Assert.Equal(postDto.IdProduct, result.IdProduct);
        Assert.Equal(postDto.Stock, result.Stock);
        await _centerProductRepository.Received(1).AddAsync(
            Arg.Is<CenterProduct>(cp => cp.IdProduct == postDto.IdProduct && cp.IdCenter == postDto.IdCenter),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_AllowsNullOptionalForeignKeys()
    {
        var postDto = new CenterProductPostDto
        {
            IdProduct = 1,
            IdCenter = 1,
            PackQuantity = 10m,
            Moq = 5m,
            LeadTime = 7,
            Frequency = 30,
            Stock = 100m
        };
        _centerProductRepository.AddAsync(Arg.Any<CenterProduct>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<CenterProduct>());

        var result = await _sut.AddAsync(postDto);

        Assert.Null(result.IdOriginCenter);
        Assert.Null(result.IdProvider);
        Assert.Null(result.IdTag);
        Assert.Null(result.IdReason);
        Assert.Null(result.IdAllocationGroup);
        Assert.Null(result.IdBufferProfile);
    }

    [Fact]
    public async Task AddAsync_ThrowsBadRequestException_WhenProductDoesNotExist()
    {
        var postDto = BuildPostDto();
        _productRepository.Exists(postDto.IdProduct, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddAsync(postDto));

        await _centerProductRepository.DidNotReceive().AddAsync(Arg.Any<CenterProduct>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_ThrowsBadRequestException_WhenOriginCenterDoesNotExist()
    {
        var postDto = BuildPostDto();
        _centerRepository.Exists(postDto.IdOriginCenter!.Value, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddAsync(postDto));
    }

    [Fact]
    public async Task AddAsync_ThrowsBadRequestException_WhenProviderDoesNotExist()
    {
        var postDto = BuildPostDto();
        _partnerRepository.Exists(postDto.IdProvider!.Value, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddAsync(postDto));
    }

    [Fact]
    public async Task AddAsync_ThrowsBadRequestException_WhenTagDoesNotExist()
    {
        var postDto = BuildPostDto();
        _tagRepository.Exists(postDto.IdTag!.Value, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddAsync(postDto));
    }

    [Fact]
    public async Task AddAsync_ThrowsBadRequestException_WhenReasonDoesNotExist()
    {
        var postDto = BuildPostDto();
        _reasonRepository.Exists(postDto.IdReason!.Value, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddAsync(postDto));
    }

    [Fact]
    public async Task AddAsync_ThrowsBadRequestException_WhenAllocationGroupDoesNotExist()
    {
        var postDto = BuildPostDto();
        _allocationGroupRepository.Exists(postDto.IdAllocationGroup!.Value, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddAsync(postDto));
    }

    [Fact]
    public async Task AddAsync_ThrowsBadRequestException_WhenBufferProfileDoesNotExist()
    {
        var postDto = BuildPostDto();
        _bufferProfileRepository.Exists(postDto.IdBufferProfile!.Value, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddAsync(postDto));
    }

    [Fact]
    public async Task AddAsync_AllowsNullBufferProfile()
    {
        var postDto = new CenterProductPostDto
        {
            IdProduct = 1,
            IdCenter = 1,
            PackQuantity = 10m,
            Moq = 5m,
            LeadTime = 7,
            Frequency = 30,
            Stock = 100m
        };
        _centerProductRepository.AddAsync(Arg.Any<CenterProduct>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<CenterProduct>());

        var result = await _sut.AddAsync(postDto);

        Assert.Null(result.IdBufferProfile);
    }

    [Fact]
    public async Task UpdateAsync_AppliesChanges_ButKeepsIdProductAndIdCenter_WhenCenterProductExists()
    {
        var existing = new CenterProduct { Id = 1, IdProduct = 1, IdCenter = 1, PackQuantity = 5m, Moq = 2m, Stock = 50m };
        var putDto = new CenterProductPutDto
        {
            PackQuantity = 20m,
            Moq = 10m,
            LeadTime = 5,
            Frequency = 15,
            Stock = 200m
        };
        _centerProductRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _centerProductRepository.UpdateAsync(Arg.Any<CenterProduct>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<CenterProduct>());

        var result = await _sut.UpdateAsync(1, putDto);

        Assert.Equal(20m, result.PackQuantity);
        Assert.Equal(200m, result.Stock);
        Assert.Equal(1, result.IdProduct);
        Assert.Equal(1, result.IdCenter);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsBadRequestException_WhenOriginCenterDoesNotExist()
    {
        var putDto = new CenterProductPutDto { IdOriginCenter = 99, PackQuantity = 1m, Moq = 1m, Stock = 1m };
        _centerRepository.Exists(99, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.UpdateAsync(1, putDto));

        await _centerProductRepository.DidNotReceive().GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ThrowsNotFoundException_WhenCenterProductDoesNotExist()
    {
        _centerProductRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns((CenterProduct)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(1));
    }
}
