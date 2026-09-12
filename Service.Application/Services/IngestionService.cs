using Service.Application.DTOs.Ingestion;
using Service.Application.Exceptions;
using Service.Application.Ingestion;
using Service.Application.Interfaces;
using Service.Domain.Ingestion;
using Service.Domain.Interfaces;

namespace Service.Application.Services
{
    public class IngestionService : IIngestionService
    {
        private readonly IIngestionConfigProvider _configProvider;
        private readonly IEnumerable<IIngestionSourceReader> _readers;
        private readonly IEnumerable<IIngestionWriter> _writers;
        private readonly ILookupValueProvider _lookupProvider;

        public IngestionService(
            IIngestionConfigProvider configProvider,
            IEnumerable<IIngestionSourceReader> readers,
            IEnumerable<IIngestionWriter> writers,
            ILookupValueProvider lookupProvider)
        {
            _configProvider = configProvider;
            _readers = readers;
            _writers = writers;
            _lookupProvider = lookupProvider;
        }

        public async Task<List<IngestionRunResultDto>> RunAsync(string? view, CancellationToken cancellationToken = default)
        {
            List<IngestionSourceConfig> sources;
            try
            {
                sources = await _configProvider.GetSourcesAsync(cancellationToken);
            }
            catch (FileNotFoundException ex)
            {
                throw new BadRequestException(ex.Message);
            }

            if (!string.IsNullOrWhiteSpace(view))
                sources = sources.Where(s => string.Equals(s.View, view, StringComparison.OrdinalIgnoreCase)).ToList();

            if (sources.Count == 0)
                throw new BadRequestException($"No ingestion source configured for view '{view}'.");

            var results = new List<IngestionRunResultDto>();
            foreach (var source in sources)
                results.Add(await RunSourceAsync(source, cancellationToken));

            return results;
        }

        private async Task<IngestionRunResultDto> RunSourceAsync(IngestionSourceConfig source, CancellationToken cancellationToken)
        {
            var reader = _readers.FirstOrDefault(r => r.CanHandle(source.Type))
                ?? throw new BadRequestException($"Unsupported ingestion source type '{source.Type}' for view '{source.View}'.");

            var writer = _writers.FirstOrDefault(w => w.CanHandle(source.View))
                ?? throw new BadRequestException($"Unsupported ingestion view '{source.View}'.");

            ValidateKeyMappings(source);

            var rawRows = new List<IReadOnlyDictionary<string, string?>>();
            try
            {
                await foreach (var row in reader.ReadAsync(source, cancellationToken))
                    rawRows.Add(row);
            }
            catch (FileNotFoundException ex)
            {
                throw new BadRequestException(ex.Message);
            }

            Dictionary<(string Entity, string By), Dictionary<string, string>> resolvedLookups;
            try
            {
                resolvedLookups = await ResolveLookupsAsync(source, rawRows, cancellationToken);
            }
            catch (NotSupportedException ex)
            {
                throw new BadRequestException(ex.Message);
            }
            var mapped = FieldMappingEngine.MapRows(rawRows, source, resolvedLookups);

            var validRows = mapped.Where(m => m.Error == null).Select(m => m.Values).ToList();
            var errors = mapped.Where(m => m.Error != null).Select(m => m.Error!).ToList();

            var writeResult = validRows.Count > 0
                ? await writer.WriteAsync(validRows, source.DeleteNonSent, cancellationToken)
                : new IngestionWriteResult();

            errors.AddRange(writeResult.Errors);

            return new IngestionRunResultDto
            {
                View = source.View,
                RowsRead = rawRows.Count,
                RowsInserted = writeResult.Inserted,
                RowsUpdated = writeResult.Updated,
                RowsDeleted = writeResult.Deleted,
                RowsFailed = errors.Count,
                Errors = errors
            };
        }

        private static void ValidateKeyMappings(IngestionSourceConfig source)
        {
            foreach (var keyField in source.Key)
            {
                var mapping = source.FieldMappings.FirstOrDefault(f => string.Equals(f.Target, keyField, StringComparison.OrdinalIgnoreCase));

                if (mapping == null)
                    throw new BadRequestException($"View '{source.View}' declares '{keyField}' as key but has no fieldMapping targeting it.");

                if (mapping.Source == null)
                    throw new BadRequestException($"View '{source.View}' maps key field '{keyField}' to a constant (no 'source') — a join key can't be a fixed value shared by every row.");
            }
        }

        private async Task<Dictionary<(string Entity, string By), Dictionary<string, string>>> ResolveLookupsAsync(
            IngestionSourceConfig source,
            List<IReadOnlyDictionary<string, string?>> rawRows,
            CancellationToken cancellationToken)
        {
            var resolved = new Dictionary<(string, string), Dictionary<string, string>>();

            var lookupGroups = source.FieldMappings
                .Where(f => f.Lookup != null)
                .GroupBy(f => (f.Lookup!.Entity, f.Lookup.By));

            foreach (var group in lookupGroups)
            {
                var distinctValues = rawRows
                    .SelectMany(r => group.Select(mapping => mapping.Source != null && r.TryGetValue(mapping.Source, out var v) ? v : null))
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Select(v => v!)
                    .Distinct()
                    .ToList();

                resolved[group.Key] = await _lookupProvider.ResolveAsync(group.Key.Entity, group.Key.By, distinctValues, cancellationToken);
            }

            return resolved;
        }
    }
}
