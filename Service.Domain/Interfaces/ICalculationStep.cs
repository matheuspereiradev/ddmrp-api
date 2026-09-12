using Service.Domain.Calculation;

namespace Service.Domain.Interfaces
{
    public interface ICalculationStep
    {
        bool CanHandle(string name);
        Task<CalculationStepResult> ExecuteAsync(CalculationStepConfig step, CancellationToken cancellationToken = default);
    }
}
