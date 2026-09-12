using Service.Application.DTOs.Calculation;
using Service.Application.DTOs.Ingestion;

namespace Service.Application.DTOs.Robot
{
    public class RobotRunResultDto
    {
        public List<IngestionRunResultDto> Ingestion { get; set; } = [];
        public List<CalculationStepResultDto> Calculation { get; set; } = [];
    }
}
