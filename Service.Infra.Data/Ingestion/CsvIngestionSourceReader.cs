using System.Globalization;
using System.Runtime.CompilerServices;
using CsvHelper;
using CsvHelper.Configuration;
using Service.Domain.Ingestion;
using Service.Domain.Interfaces;

namespace Service.Infra.Data.Ingestion
{
    public class CsvIngestionSourceReader : IIngestionSourceReader
    {
        public bool CanHandle(string type) => string.Equals(type, "Csv", StringComparison.OrdinalIgnoreCase);

        public async IAsyncEnumerable<IReadOnlyDictionary<string, string?>> ReadAsync(
            IngestionSourceConfig source,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var path = Path.IsPathRooted(source.Path) ? source.Path : Path.Combine(Directory.GetCurrentDirectory(), source.Path);

            if (!File.Exists(path))
                throw new FileNotFoundException($"CSV file not found for view '{source.View}': {path}");

            using var streamReader = new StreamReader(path);
            using var csv = new CsvReader(streamReader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null
            });

            await csv.ReadAsync();
            csv.ReadHeader();
            var headers = csv.HeaderRecord ?? [];

            while (await csv.ReadAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var row = new Dictionary<string, string?>();
                foreach (var header in headers)
                    row[header] = csv.GetField(header);

                yield return row;
            }
        }
    }
}
