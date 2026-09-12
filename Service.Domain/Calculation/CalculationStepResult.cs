namespace Service.Domain.Calculation
{
    public class CalculationStepResult
    {
        public string Name { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Error { get; set; }
        public long DurationMs { get; set; }
    }
}
