namespace Service.Domain.Calculation
{
    public class CalculationStepConfig
    {
        public string Name { get; set; } = string.Empty;
        public List<CalculationParameterConfig> Parameters { get; set; } = [];
    }
}
