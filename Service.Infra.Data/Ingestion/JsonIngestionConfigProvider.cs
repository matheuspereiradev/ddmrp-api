using System.Text.Json;
using Service.Domain.Ingestion;
using Service.Domain.Interfaces;

namespace Service.Infra.Data.Ingestion
{
    public class JsonIngestionConfigProvider : IIngestionConfigProvider
    {
        private readonly string _configPath;

        public JsonIngestionConfigProvider(string configPath)
        {
            _configPath = configPath;
        }

        public async Task<List<IngestionSourceConfig>> GetSourcesAsync(CancellationToken cancellationToken = default)
        {
            var path = Path.IsPathRooted(_configPath) ? _configPath : Path.Combine(Directory.GetCurrentDirectory(), _configPath);

            if (!File.Exists(path))
                throw new FileNotFoundException($"Ingestion config file not found: {path}");

            await using var stream = File.OpenRead(path);
            var document = await JsonSerializer.DeserializeAsync<IngestionConfigDocument>(
                stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);

            return document?.Sources ?? [];
        }

        private class IngestionConfigDocument
        {
            public List<IngestionSourceConfig> Sources { get; set; } = [];
        }
    }
}
