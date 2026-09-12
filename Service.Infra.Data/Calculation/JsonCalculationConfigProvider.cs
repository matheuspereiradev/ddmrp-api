using System.Text.Json;
using Service.Domain.Calculation;
using Service.Domain.Interfaces;

namespace Service.Infra.Data.Calculation
{
    public class JsonCalculationConfigProvider : ICalculationConfigProvider
    {
        private readonly string _configPath;

        public JsonCalculationConfigProvider(string configPath)
        {
            _configPath = configPath;
        }

        public async Task<List<CalculationStepConfig>> GetStepsAsync(CancellationToken cancellationToken = default)
        {
            var path = Path.IsPathRooted(_configPath) ? _configPath : Path.Combine(Directory.GetCurrentDirectory(), _configPath);

            if (!File.Exists(path))
                throw new FileNotFoundException($"Calculation config file not found: {path}");

            await using var stream = File.OpenRead(path);
            var document = await JsonSerializer.DeserializeAsync<CalculationConfigDocument>(
                stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);

            return document?.Steps ?? [];
        }

        private class CalculationConfigDocument
        {
            public List<CalculationStepConfig> Steps { get; set; } = [];
        }
    }
}
