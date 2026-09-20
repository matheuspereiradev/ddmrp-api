using NSubstitute;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Entities;
using Service.Domain.Enums;
using Service.Domain.Interfaces;
using Service.Domain.Report.Results;

namespace Service.Tests.Application;

public class ReportServiceTests
{
    private readonly IReportRepository _reportRepository = Substitute.For<IReportRepository>();
    private readonly ICenterProductRepository _centerProductRepository = Substitute.For<ICenterProductRepository>();
    private readonly ReportService _sut;

    public ReportServiceTests()
    {
        _sut = new ReportService(_reportRepository, _centerProductRepository);
    }

    [Fact]
    public void GetInventoryBufferManagementQueryable_ReturnsWhatTheRepositoryReturns()
    {
        var rows = new List<InventoryBufferManagementRow> { new() { CenterCode = "C1" } }.AsQueryable();
        _reportRepository.GetInventoryBufferManagementQueryable().Returns(rows);

        var result = _sut.GetInventoryBufferManagementQueryable();

        Assert.Same(rows, result);
    }

    [Fact]
    public async Task GetOpenOrdersAsync_ReturnsWhatTheRepositoryReturns()
    {
        var rows = new List<OpenOrderRow> { new() { OrderNumber = "OR1" } };
        _reportRepository.GetOpenOrdersAsync(1, 2, Arg.Any<CancellationToken>()).Returns(rows);

        var result = await _sut.GetOpenOrdersAsync(1, 2);

        Assert.Same(rows, result);
    }

    [Fact]
    public async Task GetProjectedStockAlertAsync_ReturnsWhatTheRepositoryReturns_WhenCenterProductExists()
    {
        var dateStart = new DateTime(2026, 9, 1);
        var dateEnd = new DateTime(2026, 9, 30);
        var rows = new List<ProjectedStockAlertRow> { new() { IdProduct = 2, IdCenter = 1 } };
        _centerProductRepository.GetByProductAndCenterAsync(2, 1, Arg.Any<CancellationToken>()).Returns(new CenterProduct { IdProduct = 2, IdCenter = 1 });
        _reportRepository.GetProjectedStockAlertAsync(1, 2, dateStart, dateEnd, true, true, true, true, false, false, true, Arg.Any<CancellationToken>()).Returns(rows);

        var result = await _sut.GetProjectedStockAlertAsync(1, 2, dateStart, dateEnd);

        Assert.Same(rows, result);
    }

    [Fact]
    public async Task GetProjectedStockAlertAsync_ThrowsNotFound_WhenCenterProductDoesNotExist()
    {
        var dateStart = new DateTime(2026, 9, 1);
        var dateEnd = new DateTime(2026, 9, 30);
        _centerProductRepository.GetByProductAndCenterAsync(2, 1, Arg.Any<CancellationToken>()).Returns((CenterProduct)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetProjectedStockAlertAsync(1, 2, dateStart, dateEnd));
    }

    [Fact]
    public async Task GetProjectedStockAlertAsync_ThrowsBadRequest_WhenDateEndIsBeforeDateStart()
    {
        var dateStart = new DateTime(2026, 9, 30);
        var dateEnd = new DateTime(2026, 9, 1);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.GetProjectedStockAlertAsync(1, 2, dateStart, dateEnd));
    }

    [Fact]
    public async Task GetProjectedStockAlertAsync_PassesToggleFlagsThroughToTheRepository()
    {
        var dateStart = new DateTime(2026, 9, 1);
        var dateEnd = new DateTime(2026, 9, 30);
        var rows = new List<ProjectedStockAlertRow>();
        _centerProductRepository.GetByProductAndCenterAsync(2, 1, Arg.Any<CancellationToken>()).Returns(new CenterProduct { IdProduct = 2, IdCenter = 1 });
        _reportRepository.GetProjectedStockAlertAsync(1, 2, dateStart, dateEnd, false, false, false, false, true, true, false, Arg.Any<CancellationToken>()).Returns(rows);

        var result = await _sut.GetProjectedStockAlertAsync(
            1, 2, dateStart, dateEnd,
            useAdu: false, useForecast: false, useInbounds: false, useOutbounds: false,
            accumulateInboundsToday: true, accumulateOutboundsToday: true, useFictionalOrders: false);

        Assert.Same(rows, result);
    }

    [Fact]
    public async Task GetBufferPenetrationAsync_ReturnsWhatTheRepositoryReturns()
    {
        var dateStart = new DateTime(2026, 9, 1);
        var dateEnd = new DateTime(2026, 9, 30);
        var idCenters = new[] { 1 };
        var rows = new List<BufferPenetrationRow> { new() { IdProduct = 2, IdCenter = 1 } };
        _reportRepository.GetBufferPenetrationAsync(dateStart, dateEnd, idCenters, 2, BufferPenetrationMode.Netflow, Arg.Any<CancellationToken>()).Returns(rows);

        var result = await _sut.GetBufferPenetrationAsync(dateStart, dateEnd, idCenters, 2);

        Assert.Same(rows, result);
    }

    [Fact]
    public async Task GetBufferPenetrationAsync_PassesModeThroughToTheRepository()
    {
        var dateStart = new DateTime(2026, 9, 1);
        var dateEnd = new DateTime(2026, 9, 30);
        var idCenters = new[] { 1 };
        var rows = new List<BufferPenetrationRow> { new() { IdProduct = 2, IdCenter = 1 } };
        _reportRepository.GetBufferPenetrationAsync(dateStart, dateEnd, idCenters, 2, BufferPenetrationMode.Execution, Arg.Any<CancellationToken>()).Returns(rows);

        var result = await _sut.GetBufferPenetrationAsync(dateStart, dateEnd, idCenters, 2, BufferPenetrationMode.Execution);

        Assert.Same(rows, result);
    }

    [Fact]
    public async Task GetBufferPenetrationAsync_ThrowsBadRequest_WhenDateEndIsBeforeDateStart()
    {
        var dateStart = new DateTime(2026, 9, 30);
        var dateEnd = new DateTime(2026, 9, 1);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.GetBufferPenetrationAsync(dateStart, dateEnd, null, null));
    }

    [Fact]
    public async Task GetItemsByBufferColorHistoryAsync_ReturnsWhatTheRepositoryReturns()
    {
        var dateStart = new DateTime(2026, 9, 1);
        var dateEnd = new DateTime(2026, 9, 30);
        var idCenters = new[] { 1 };
        var repositoryResult = new ItemsByBufferColorHistoryResult
        {
            Netflow = new List<BufferColorHistoryDayRow> { new() { Red = 1 } }
        };
        _reportRepository.GetItemsByBufferColorHistoryAsync(dateStart, dateEnd, idCenters, 2, Arg.Any<CancellationToken>()).Returns(repositoryResult);

        var result = await _sut.GetItemsByBufferColorHistoryAsync(dateStart, dateEnd, idCenters, 2);

        Assert.Same(repositoryResult, result);
    }

    [Fact]
    public async Task GetItemsByBufferColorHistoryAsync_ThrowsBadRequest_WhenDateEndIsBeforeDateStart()
    {
        var dateStart = new DateTime(2026, 9, 30);
        var dateEnd = new DateTime(2026, 9, 1);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.GetItemsByBufferColorHistoryAsync(dateStart, dateEnd, null, null));
    }

    [Fact]
    public async Task GetAccumulatedBufferHistoryAsync_ReturnsWhatTheRepositoryReturns()
    {
        var dateStart = new DateTime(2026, 9, 1);
        var dateEnd = new DateTime(2026, 9, 30);
        var idCenters = new[] { 1, 2 };
        var rows = new List<AccumulatedBufferHistoryRow> { new() { AvailableStock = 100m } };
        _reportRepository.GetAccumulatedBufferHistoryAsync(dateStart, dateEnd, idCenters, Arg.Any<CancellationToken>()).Returns(rows);

        var result = await _sut.GetAccumulatedBufferHistoryAsync(dateStart, dateEnd, idCenters);

        Assert.Same(rows, result);
    }

    [Fact]
    public async Task GetAccumulatedBufferHistoryAsync_ThrowsBadRequest_WhenDateEndIsBeforeDateStart()
    {
        var dateStart = new DateTime(2026, 9, 30);
        var dateEnd = new DateTime(2026, 9, 1);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.GetAccumulatedBufferHistoryAsync(dateStart, dateEnd, new[] { 1 }));
    }

    [Fact]
    public async Task GetAccumulatedBufferHistoryAsync_ThrowsBadRequest_WhenIdCentersIsNullOrEmpty()
    {
        var dateStart = new DateTime(2026, 9, 1);
        var dateEnd = new DateTime(2026, 9, 30);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.GetAccumulatedBufferHistoryAsync(dateStart, dateEnd, null!));
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.GetAccumulatedBufferHistoryAsync(dateStart, dateEnd, Array.Empty<int>()));
    }
}
