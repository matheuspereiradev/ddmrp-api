using NSubstitute;
using Service.Application.Services;
using Service.Domain.Interfaces;
using Service.Domain.Report.Results;

namespace Service.Tests.Application;

public class ReportServiceTests
{
    private readonly IReportRepository _reportRepository = Substitute.For<IReportRepository>();
    private readonly ReportService _sut;

    public ReportServiceTests()
    {
        _sut = new ReportService(_reportRepository);
    }

    [Fact]
    public async Task GetInventoryBufferManagementAsync_ReturnsWhatTheRepositoryReturns()
    {
        var rows = new List<InventoryBufferManagementRow> { new() { CenterCode = "C1" } };
        _reportRepository.GetInventoryBufferManagementAsync(Arg.Any<CancellationToken>()).Returns(rows);

        var result = await _sut.GetInventoryBufferManagementAsync();

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
}
