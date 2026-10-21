using Service.Application.DTOs.Forecast;

namespace Service.Application.DTOs.ForecastAI
{
    public class ForecastAIApplyResultDto
    {
        public ForecastAIGetDto AppliedPreview { get; set; }
        public ForecastGetDto Forecast { get; set; }
    }
}
