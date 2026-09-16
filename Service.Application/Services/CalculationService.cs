using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Domain.Calculation;
using Service.Domain.Interfaces;

namespace Service.Application.Services
{
    public class CalculationService : ICalculationService
    {
        private readonly ICalculationConfigProvider _configProvider;
        private readonly IEnumerable<ICalculationStep> _steps;
        private readonly ICenterProductRepository _centerProductRepository;

        public CalculationService(ICalculationConfigProvider configProvider, IEnumerable<ICalculationStep> steps, ICenterProductRepository centerProductRepository)
        {
            _configProvider = configProvider;
            _steps = steps;
            _centerProductRepository = centerProductRepository;
        }

        public async Task<List<CalculationStepResult>> RunAsync(int? idCenterProduct = null, CancellationToken cancellationToken = default)
        {
            if (idCenterProduct.HasValue && !await _centerProductRepository.Exists(idCenterProduct.Value, cancellationToken))
                throw new NotFoundException("CenterProduct not found.");

            List<CalculationStepConfig> configuredSteps;
            try
            {
                configuredSteps = await _configProvider.GetStepsAsync(cancellationToken);
            }
            catch (FileNotFoundException ex)
            {
                throw new BadRequestException(ex.Message);
            }

            var results = new List<CalculationStepResult>();

            foreach (var stepConfig in configuredSteps)
            {
                var step = _steps.FirstOrDefault(s => s.CanHandle(stepConfig.Name))
                    ?? throw new BadRequestException($"Unsupported calculation step '{stepConfig.Name}'.");

                var result = await step.ExecuteAsync(stepConfig, idCenterProduct, cancellationToken);
                results.Add(result);

                if (!result.Success)
                    break;
            }

            return results;
        }
    }
}
