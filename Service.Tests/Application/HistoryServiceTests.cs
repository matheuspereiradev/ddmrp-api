using NSubstitute;
using Service.Application.DTOs.History;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Entities;
using Service.Domain.Enums;
using Service.Domain.Interfaces;

namespace Service.Tests.Application;

public class HistoryServiceTests
{
    private readonly IHistoryRepository _historyRepository = Substitute.For<IHistoryRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ICenterRepository _centerRepository = Substitute.For<ICenterRepository>();
    private readonly HistoryService _sut;

    public HistoryServiceTests()
    {
        _productRepository.Exists(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        _centerRepository.Exists(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        _sut = new HistoryService(_historyRepository, _productRepository, _centerRepository);
    }

    private static HistoryPostDto BuildPostDto() => new()
    {
        IdProduct = 1,
        IdCenter = 1,
        Quantity = 100.5m,
        Date = new DateTime(2026, 9, 11)
    };

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenHistoryExists()
    {
        var history = new History { Id = 1, IdProduct = 1, IdCenter = 1, Quantity = 10m, Date = DateTime.UtcNow };
        _historyRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(history);

        var result = await _sut.GetByIdAsync(1);

        Assert.Equal(history.Id, result.Id);
        Assert.Equal(history.IdProduct, result.IdProduct);
        Assert.Equal(history.IdCenter, result.IdCenter);
    }

    [Fact]
    public async Task GetByIdAsync_IncludesProductAndCenter_WhenLoaded()
    {
        var history = new History
        {
            Id = 1,
            IdProduct = 1,
            IdCenter = 2,
            Quantity = 10m,
            Date = DateTime.UtcNow,
            Product = new Product { Id = 1, Reference = "REF001", Description = "Produto Teste", UnitOfMeasure = "UN" },
            Center = new Center { Id = 2, Code = "C001", Description = "Centro Teste" }
        };
        _historyRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(history);

        var result = await _sut.GetByIdAsync(1);

        Assert.NotNull(result.Product);
        Assert.Equal("REF001", result.Product!.Reference);
        Assert.NotNull(result.Center);
        Assert.Equal("C001", result.Center!.Code);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFoundException_WhenHistoryDoesNotExist()
    {
        _historyRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((History)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(1));
    }

    [Fact]
    public async Task AddAsync_PersistsMappedEntity_WhenProductAndCenterExist()
    {
        var postDto = BuildPostDto();
        _historyRepository.AddAsync(Arg.Any<History>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<History>());

        var result = await _sut.AddAsync(postDto);

        Assert.Equal(postDto.IdProduct, result.IdProduct);
        Assert.Equal(postDto.Quantity, result.Quantity);
        Assert.Equal(DiscardStatus.NotReviewed, result.DiscardStatus);
        await _historyRepository.Received(1).AddAsync(
            Arg.Is<History>(h => h.IdProduct == postDto.IdProduct && h.IdCenter == postDto.IdCenter && h.DiscardStatus == DiscardStatus.NotReviewed),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_ThrowsBadRequestException_WhenProductDoesNotExist()
    {
        var postDto = BuildPostDto();
        _productRepository.Exists(postDto.IdProduct, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddAsync(postDto));

        await _historyRepository.DidNotReceive().AddAsync(Arg.Any<History>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_ThrowsBadRequestException_WhenCenterDoesNotExist()
    {
        var postDto = BuildPostDto();
        _centerRepository.Exists(postDto.IdCenter, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddAsync(postDto));

        await _historyRepository.DidNotReceive().AddAsync(Arg.Any<History>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_UpdatesQuantityAndDiscardStatus_ButKeepsIdProductIdCenterAndDate()
    {
        var existing = new History { Id = 1, IdProduct = 1, IdCenter = 1, Quantity = 10m, Date = new DateTime(2026, 9, 11), DiscardStatus = DiscardStatus.NotReviewed };
        var putDto = new HistoryPutDto { Quantity = 200m, DiscardStatus = DiscardStatus.Discarded };
        _historyRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _historyRepository.UpdateAsync(Arg.Any<History>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<History>());

        var result = await _sut.UpdateAsync(1, putDto);

        Assert.Equal(200m, result.Quantity);
        Assert.Equal(DiscardStatus.Discarded, result.DiscardStatus);
        Assert.Equal(1, result.IdProduct);
        Assert.Equal(1, result.IdCenter);
        Assert.Equal(new DateTime(2026, 9, 11), result.Date);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsNotFoundException_WhenHistoryDoesNotExist()
    {
        _historyRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns((History)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(1));
    }
}
