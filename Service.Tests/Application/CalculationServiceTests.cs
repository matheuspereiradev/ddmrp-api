using NSubstitute;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Calculation;
using Service.Domain.Interfaces;

namespace Service.Tests.Application;

public class CalculationServiceTests
{
    private readonly ICalculationConfigProvider _configProvider = Substitute.For<ICalculationConfigProvider>();
    private readonly ICalculationStep _step = Substitute.For<ICalculationStep>();
    private readonly CalculationService _sut;

    public CalculationServiceTests()
    {
        _step.CanHandle(Arg.Any<string>()).Returns(true);
        _sut = new CalculationService(_configProvider, [_step]);
    }

    [Fact]
    public async Task RunAsync_ExecutesStepsInConfiguredOrder()
    {
        var steps = new List<CalculationStepConfig>
        {
            new() { Name = "Step1" },
            new() { Name = "Step2" },
            new() { Name = "Step3" }
        };
        _configProvider.GetStepsAsync(Arg.Any<CancellationToken>()).Returns(steps);
        _step.ExecuteAsync(Arg.Any<CalculationStepConfig>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => new CalculationStepResult { Name = callInfo.Arg<CalculationStepConfig>().Name, Success = true });

        var results = await _sut.RunAsync();

        Assert.Equal(3, results.Count);
        Assert.Equal(["Step1", "Step2", "Step3"], results.Select(r => r.Name));
        Received.InOrder(() =>
        {
            _step.ExecuteAsync(Arg.Is<CalculationStepConfig>(s => s.Name == "Step1"), Arg.Any<CancellationToken>());
            _step.ExecuteAsync(Arg.Is<CalculationStepConfig>(s => s.Name == "Step2"), Arg.Any<CancellationToken>());
            _step.ExecuteAsync(Arg.Is<CalculationStepConfig>(s => s.Name == "Step3"), Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task RunAsync_StopsAtFirstFailingStep_AndDoesNotRunRemainingSteps()
    {
        var steps = new List<CalculationStepConfig>
        {
            new() { Name = "Step1" },
            new() { Name = "Step2" },
            new() { Name = "Step3" }
        };
        _configProvider.GetStepsAsync(Arg.Any<CancellationToken>()).Returns(steps);
        _step.ExecuteAsync(Arg.Is<CalculationStepConfig>(s => s.Name == "Step1"), Arg.Any<CancellationToken>())
            .Returns(new CalculationStepResult { Name = "Step1", Success = true });
        _step.ExecuteAsync(Arg.Is<CalculationStepConfig>(s => s.Name == "Step2"), Arg.Any<CancellationToken>())
            .Returns(new CalculationStepResult { Name = "Step2", Success = false, Error = "boom" });

        var results = await _sut.RunAsync();

        Assert.Equal(2, results.Count);
        Assert.True(results[0].Success);
        Assert.False(results[1].Success);
        await _step.DidNotReceive().ExecuteAsync(Arg.Is<CalculationStepConfig>(s => s.Name == "Step3"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_ThrowsBadRequestException_WhenConfigFileIsMissing()
    {
        _configProvider.GetStepsAsync(Arg.Any<CancellationToken>()).Returns<Task<List<CalculationStepConfig>>>(_ => throw new FileNotFoundException("not found"));

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.RunAsync());
    }

    [Fact]
    public async Task RunAsync_ThrowsBadRequestException_WhenNoStepHandlesTheConfiguredName()
    {
        _step.CanHandle(Arg.Any<string>()).Returns(false);
        _configProvider.GetStepsAsync(Arg.Any<CancellationToken>()).Returns([new CalculationStepConfig { Name = "Unknown" }]);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.RunAsync());
    }
}
