using Service.Domain.Calculation;

namespace Service.Domain.Interfaces
{
    public interface ICalculationConfigProvider
    {
        Task<List<CalculationStepConfig>> GetStepsAsync(CancellationToken cancellationToken = default);
    }
}
