using Service.Application.DTOs.Calculation;
using Service.Application.DTOs.Robot;
using Service.Application.Interfaces;

namespace Service.Application.Services
{
    public class RobotService : IRobotService
    {
        private readonly IIngestionService _ingestionService;
        private readonly ICalculationService _calculationService;

        public RobotService(IIngestionService ingestionService, ICalculationService calculationService)
        {
            _ingestionService = ingestionService;
            _calculationService = calculationService;
        }

        public async Task<RobotRunResultDto> RunAsync(CancellationToken cancellationToken = default)
        {
            var ingestionResults = await _ingestionService.RunAsync(view: null, cancellationToken);
            var calculationResults = await _calculationService.RunAsync(idCenterProduct: null, cancellationToken);

            return new RobotRunResultDto
            {
                Ingestion = ingestionResults,
                Calculation = calculationResults.Select(r => new CalculationStepResultDto
                {
                    Name = r.Name,
                    Success = r.Success,
                    Error = r.Error,
                    DurationMs = r.DurationMs
                }).ToList()
            };
        }
    }
}
