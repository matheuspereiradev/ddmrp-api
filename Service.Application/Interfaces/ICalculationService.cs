using Service.Domain.Calculation;

namespace Service.Application.Interfaces
{
    public interface ICalculationService
    {
        Task<List<CalculationStepResult>> RunAsync(CancellationToken cancellationToken = default);
    }
}
