namespace Service.Application.DTOs.Calculation
{
    public class CalculationStepResultDto
    {
        public string Name { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Error { get; set; }
        public long DurationMs { get; set; }
    }
}
