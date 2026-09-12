using NSubstitute;
using Service.Application.DTOs.Ingestion;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Application.Services;
using Service.Domain.Calculation;

namespace Service.Tests.Application;

public class RobotServiceTests
{
    private readonly IIngestionService _ingestionService = Substitute.For<IIngestionService>();
    private readonly ICalculationService _calculationService = Substitute.For<ICalculationService>();
    private readonly RobotService _sut;

    public RobotServiceTests()
    {
        _sut = new RobotService(_ingestionService, _calculationService);
    }

    [Fact]
    public async Task RunAsync_RunsIngestionBeforeCalculation()
    {
        _ingestionService.RunAsync(null, Arg.Any<CancellationToken>()).Returns([new IngestionRunResultDto { View = "Centers" }]);
        _calculationService.RunAsync(Arg.Any<CancellationToken>()).Returns([new CalculationStepResult { Name = "usp_Step1", Success = true }]);

        var result = await _sut.RunAsync();

        Assert.Single(result.Ingestion);
        Assert.Single(result.Calculation);
        Received.InOrder(() =>
        {
            _ingestionService.RunAsync(null, Arg.Any<CancellationToken>());
            _calculationService.RunAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task RunAsync_DoesNotRunCalculation_WhenIngestionThrows()
    {
        _ingestionService.RunAsync(null, Arg.Any<CancellationToken>())
            .Returns<Task<List<IngestionRunResultDto>>>(_ => throw new BadRequestException("ingestion config missing"));

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.RunAsync());

        await _calculationService.DidNotReceive().RunAsync(Arg.Any<CancellationToken>());
    }
}
