using NSubstitute;
using Service.Application.DTOs.Forecast;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;

namespace Service.Tests.Application;

public class ForecastServiceTests
{
    private readonly IForecastRepository _forecastRepository = Substitute.For<IForecastRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ICenterRepository _centerRepository = Substitute.For<ICenterRepository>();
    private readonly ForecastService _sut;

    public ForecastServiceTests()
    {
        _productRepository.Exists(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        _centerRepository.Exists(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        _sut = new ForecastService(_forecastRepository, _productRepository, _centerRepository);
    }

    private static ForecastPostDto BuildPostDto() => new()
    {
        IdProduct = 1,
        IdCenter = 1,
        Quantity = 100.5m,
        Date = new DateTime(2026, 9, 11)
    };

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenForecastExists()
    {
        var forecast = new Forecast { Id = 1, IdProduct = 1, IdCenter = 1, Quantity = 10m, Date = DateTime.UtcNow };
        _forecastRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(forecast);

        var result = await _sut.GetByIdAsync(1);

        Assert.Equal(forecast.Id, result.Id);
        Assert.Equal(forecast.IdProduct, result.IdProduct);
        Assert.Equal(forecast.IdCenter, result.IdCenter);
    }

    [Fact]
    public async Task GetByIdAsync_IncludesProductAndCenter_WhenLoaded()
    {
        var forecast = new Forecast
        {
            Id = 1,
            IdProduct = 1,
            IdCenter = 2,
            Quantity = 10m,
            Date = DateTime.UtcNow,
            Product = new Product { Id = 1, Reference = "REF001", Description = "Produto Teste", UnitOfMeasure = "UN" },
            Center = new Center { Id = 2, Code = "C001", Description = "Centro Teste" }
        };
        _forecastRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(forecast);

        var result = await _sut.GetByIdAsync(1);

        Assert.NotNull(result.Product);
        Assert.Equal("REF001", result.Product!.Reference);
        Assert.NotNull(result.Center);
        Assert.Equal("C001", result.Center!.Code);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFoundException_WhenForecastDoesNotExist()
    {
        _forecastRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Forecast)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(1));
    }

    [Fact]
    public async Task AddAsync_PersistsMappedEntity_WhenProductAndCenterExist()
    {
        var postDto = BuildPostDto();
        _forecastRepository.AddAsync(Arg.Any<Forecast>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Forecast>());

        var result = await _sut.AddAsync(postDto);

        Assert.Equal(postDto.IdProduct, result.IdProduct);
        Assert.Equal(postDto.Quantity, result.Quantity);
        await _forecastRepository.Received(1).AddAsync(
            Arg.Is<Forecast>(f => f.IdProduct == postDto.IdProduct && f.IdCenter == postDto.IdCenter),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_ThrowsBadRequestException_WhenProductDoesNotExist()
    {
        var postDto = BuildPostDto();
        _productRepository.Exists(postDto.IdProduct, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddAsync(postDto));

        await _forecastRepository.DidNotReceive().AddAsync(Arg.Any<Forecast>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_ThrowsBadRequestException_WhenCenterDoesNotExist()
    {
        var postDto = BuildPostDto();
        _centerRepository.Exists(postDto.IdCenter, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddAsync(postDto));

        await _forecastRepository.DidNotReceive().AddAsync(Arg.Any<Forecast>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOnlyQuantity_WhenForecastExists()
    {
        var existing = new Forecast { Id = 1, IdProduct = 1, IdCenter = 1, Quantity = 10m, Date = new DateTime(2026, 9, 11) };
        var putDto = new ForecastPutDto { Quantity = 200m };
        _forecastRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _forecastRepository.UpdateAsync(Arg.Any<Forecast>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Forecast>());

        var result = await _sut.UpdateAsync(1, putDto);

        Assert.Equal(200m, result.Quantity);
        Assert.Equal(1, result.IdProduct);
        Assert.Equal(1, result.IdCenter);
        Assert.Equal(new DateTime(2026, 9, 11), result.Date);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsNotFoundException_WhenForecastDoesNotExist()
    {
        _forecastRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns((Forecast)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(1));
    }

    [Fact]
    public async Task GetFilteredAsync_ReturnsMappedPagedList()
    {
        var entities = new List<Forecast>
        {
            new() { Id = 1, IdProduct = 2, IdCenter = 3, Quantity = 10m, Date = new DateTime(2026, 1, 1) }
        };
        var dateStart = new DateTime(2026, 1, 1);
        var dateEnd = new DateTime(2026, 12, 31);
        _forecastRepository.GetFilteredAsync(2, 3, dateStart, dateEnd, 1, 10, Arg.Any<CancellationToken>())
            .Returns(new PagedList<Forecast>(entities, 1, 10, 1));

        var result = await _sut.GetFilteredAsync(idProduct: 2, idCenter: 3, dateStart: dateStart, dateEnd: dateEnd, pageNumber: 1, pageSize: 10);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(2, Assert.Single(result).IdProduct);
    }
}
