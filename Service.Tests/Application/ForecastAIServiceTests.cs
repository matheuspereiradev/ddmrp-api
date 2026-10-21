using NSubstitute;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Entities;
using Service.Domain.Enums;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;

namespace Service.Tests.Application;

public class ForecastAIServiceTests
{
    private readonly IForecastAIRepository _forecastAIRepository = Substitute.For<IForecastAIRepository>();
    private readonly IForecastRepository _forecastRepository = Substitute.For<IForecastRepository>();
    private readonly ForecastAIService _sut;

    public ForecastAIServiceTests()
    {
        _sut = new ForecastAIService(_forecastAIRepository, _forecastRepository);

        _forecastAIRepository.UpdateAsync(Arg.Any<ForecastAI>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<ForecastAI>());
        _forecastRepository.AddAsync(Arg.Any<Forecast>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Forecast>());
        _forecastRepository.UpdateAsync(Arg.Any<Forecast>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Forecast>());
    }

    private static ForecastAI BuildPreview(int id = 1, ForecastPreviewState state = ForecastPreviewState.NotReviewed) => new()
    {
        Id = id,
        IdProduct = 1,
        IdCenter = 2,
        MonthYear = new DateTime(2026, 10, 1),
        PreviewType = ForecastPreviewType.Optimistic,
        Assertiveness = 0.75m,
        QuantityPreviewed = 500m,
        PreviewState = state
    };

    [Fact]
    public async Task GetFilteredAsync_GroupsPreviewsByCenterAndProduct()
    {
        var center = new Center { Id = 2, Code = "C001", Description = "Centro Teste" };
        var product = new Product { Id = 1, Reference = "REF001", Description = "Produto Teste", UnitOfMeasure = "UN" };
        var entities = new List<ForecastAI>
        {
            new() { Id = 1, IdProduct = 1, IdCenter = 2, MonthYear = new DateTime(2026, 10, 1), PreviewType = ForecastPreviewType.Optimistic, Assertiveness = 0.8m, QuantityPreviewed = 600m, PreviewState = ForecastPreviewState.NotReviewed, Center = center, Product = product },
            new() { Id = 2, IdProduct = 1, IdCenter = 2, MonthYear = new DateTime(2026, 10, 1), PreviewType = ForecastPreviewType.Normal, Assertiveness = 0.7m, QuantityPreviewed = 500m, PreviewState = ForecastPreviewState.NotReviewed, Center = center, Product = product },
            new() { Id = 3, IdProduct = 1, IdCenter = 2, MonthYear = new DateTime(2026, 10, 1), PreviewType = ForecastPreviewType.Pessimistic, Assertiveness = 0.6m, QuantityPreviewed = 400m, PreviewState = ForecastPreviewState.NotReviewed, Center = center, Product = product }
        };
        _forecastAIRepository.GetFilteredAsync(2, 1, null, null, null, Arg.Any<CancellationToken>())
            .Returns(entities);

        var result = await _sut.GetFilteredAsync(idCenter: 2, idProduct: 1, monthYear: null, previewType: null, previewState: null, pageNumber: 1, pageSize: 10);

        Assert.Equal(1, result.TotalCount);
        var group = Assert.Single(result);
        Assert.Equal("C001", group.Center!.Code);
        Assert.Equal("REF001", group.Product!.Reference);
        Assert.Equal(3, group.Previews.Count);
        Assert.Contains(group.Previews, p => p.PreviewType == ForecastPreviewType.Optimistic && p.QuantityPreviewed == 600m && p.Assertiveness == 0.8m);
        Assert.Contains(group.Previews, p => p.PreviewType == ForecastPreviewType.Pessimistic && p.QuantityPreviewed == 400m);
    }

    [Fact]
    public async Task GetFilteredAsync_PaginatesOverDistinctCenterProductGroups_NotOverRawRows()
    {
        var entities = new List<ForecastAI>
        {
            BuildPreview(id: 1),
            new() { Id = 2, IdProduct = 2, IdCenter = 2, MonthYear = new DateTime(2026, 10, 1), PreviewType = ForecastPreviewType.Normal, Assertiveness = 0.7m, QuantityPreviewed = 200m }
        };
        _forecastAIRepository.GetFilteredAsync(null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(entities);

        var result = await _sut.GetFilteredAsync(idCenter: null, idProduct: null, monthYear: null, previewType: null, previewState: null, pageNumber: 1, pageSize: 1);

        Assert.Equal(2, result.TotalCount);
        Assert.Single(result);
    }

    [Fact]
    public async Task ApplyAsync_ThrowsNotFoundException_WhenPreviewDoesNotExist()
    {
        _forecastAIRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((ForecastAI)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.ApplyAsync(1));
    }

    [Fact]
    public async Task ApplyAsync_ThrowsBadRequestException_WhenPreviewAlreadyReviewed()
    {
        var preview = BuildPreview(state: ForecastPreviewState.Discarded);
        _forecastAIRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(preview);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.ApplyAsync(1));

        await _forecastRepository.DidNotReceive().AddAsync(Arg.Any<Forecast>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyAsync_InsertsForecast_WhenNoneExistsForThatMonth()
    {
        var preview = BuildPreview();
        _forecastAIRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(preview);
        _forecastRepository.GetByProductCenterAndPeriodAsync(1, 2, new DateTime(2026, 10, 1), new DateTime(2026, 10, 31), Arg.Any<CancellationToken>())
            .Returns((Forecast)null!);
        _forecastAIRepository.GetNotReviewedBySameItemAsync(1, 2, preview.MonthYear, 1, Arg.Any<CancellationToken>())
            .Returns(new List<ForecastAI>());

        var result = await _sut.ApplyAsync(1);

        Assert.Equal(500m, result.Forecast.Value);
        Assert.Equal(new DateTime(2026, 10, 1), result.Forecast.StartDate);
        Assert.Equal(new DateTime(2026, 10, 31), result.Forecast.EndDate);
        Assert.Equal(ForecastPreviewState.Applied, result.AppliedPreview.PreviewState);
        await _forecastRepository.Received(1).AddAsync(Arg.Any<Forecast>(), Arg.Any<CancellationToken>());
        await _forecastRepository.DidNotReceive().UpdateAsync(Arg.Any<Forecast>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyAsync_UpdatesExistingForecastValue_WhenOneAlreadyExistsForThatMonth()
    {
        var preview = BuildPreview();
        var existingForecast = new Forecast { Id = 9, IdProduct = 1, IdCenter = 2, Value = 100m, StartDate = new DateTime(2026, 10, 1), EndDate = new DateTime(2026, 10, 31) };
        _forecastAIRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(preview);
        _forecastRepository.GetByProductCenterAndPeriodAsync(1, 2, new DateTime(2026, 10, 1), new DateTime(2026, 10, 31), Arg.Any<CancellationToken>())
            .Returns(existingForecast);
        _forecastAIRepository.GetNotReviewedBySameItemAsync(1, 2, preview.MonthYear, 1, Arg.Any<CancellationToken>())
            .Returns(new List<ForecastAI>());

        var result = await _sut.ApplyAsync(1);

        Assert.Equal(9, result.Forecast.Id);
        Assert.Equal(500m, result.Forecast.Value);
        await _forecastRepository.DidNotReceive().AddAsync(Arg.Any<Forecast>(), Arg.Any<CancellationToken>());
        await _forecastRepository.Received(1).UpdateAsync(Arg.Is<Forecast>(f => f.Id == 9 && f.Value == 500m), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyAsync_DiscardsOtherNotReviewedPreviews_ForTheSameItemAndMonth()
    {
        var preview = BuildPreview();
        var other = BuildPreview(id: 2);
        _forecastAIRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(preview);
        _forecastRepository.GetByProductCenterAndPeriodAsync(1, 2, new DateTime(2026, 10, 1), new DateTime(2026, 10, 31), Arg.Any<CancellationToken>())
            .Returns((Forecast)null!);
        _forecastAIRepository.GetNotReviewedBySameItemAsync(1, 2, preview.MonthYear, 1, Arg.Any<CancellationToken>())
            .Returns(new List<ForecastAI> { other });

        await _sut.ApplyAsync(1);

        await _forecastAIRepository.Received(1).UpdateAsync(
            Arg.Is<ForecastAI>(f => f.Id == 2 && f.PreviewState == ForecastPreviewState.Discarded),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectAsync_DiscardsEveryNotReviewedPreview_ForTheSameItemAndMonth()
    {
        var monthYear = new DateTime(2026, 10, 1);
        var previews = new List<ForecastAI>
        {
            BuildPreview(id: 1),
            BuildPreview(id: 2),
            BuildPreview(id: 3)
        };
        _forecastAIRepository.GetNotReviewedBySameItemAsync(1, 2, monthYear, null, Arg.Any<CancellationToken>())
            .Returns(previews);

        var result = await _sut.RejectAsync(idProduct: 1, idCenter: 2, monthYear: monthYear);

        Assert.Equal(3, result.Count);
        Assert.All(result, r => Assert.Equal(ForecastPreviewState.Discarded, r.PreviewState));
        await _forecastAIRepository.Received(3).UpdateAsync(
            Arg.Is<ForecastAI>(f => f.PreviewState == ForecastPreviewState.Discarded),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectAsync_ReturnsEmptyList_WhenNoneAreNotReviewed()
    {
        var monthYear = new DateTime(2026, 10, 1);
        _forecastAIRepository.GetNotReviewedBySameItemAsync(1, 2, monthYear, null, Arg.Any<CancellationToken>())
            .Returns(new List<ForecastAI>());

        var result = await _sut.RejectAsync(idProduct: 1, idCenter: 2, monthYear: monthYear);

        Assert.Empty(result);
        await _forecastAIRepository.DidNotReceive().UpdateAsync(Arg.Any<ForecastAI>(), Arg.Any<CancellationToken>());
    }
}
