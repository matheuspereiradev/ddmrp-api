using Service.Domain.Ingestion;

namespace Service.Application.Ingestion
{
    public static class FieldMappingEngine
    {
        public static List<MappedRowResult> MapRows(
            IReadOnlyList<IReadOnlyDictionary<string, string?>> rawRows,
            IngestionSourceConfig source,
            IReadOnlyDictionary<(string Entity, string By), Dictionary<string, string>> resolvedLookups)
        {
            var results = new List<MappedRowResult>(rawRows.Count);

            foreach (var rawRow in rawRows)
            {
                var mapped = new Dictionary<string, string?>();
                string? error = null;

                foreach (var mapping in source.FieldMappings)
                {
                    var raw = mapping.Source != null && rawRow.TryGetValue(mapping.Source, out var value) ? value : null;

                    if (mapping.Lookup != null)
                    {
                        if (string.IsNullOrWhiteSpace(raw))
                        {
                            if (!mapping.Required)
                                continue;

                            error ??= $"Missing value for lookup field '{mapping.Source}' (target '{mapping.Target}').";
                            continue;
                        }

                        if (!resolvedLookups.TryGetValue((mapping.Lookup.Entity, mapping.Lookup.By), out var lookupMap) ||
                            !lookupMap.TryGetValue(raw, out var resolvedId))
                        {
                            error ??= $"Could not resolve {mapping.Lookup.Entity}.{mapping.Lookup.By} = '{raw}' (target '{mapping.Target}').";
                            continue;
                        }

                        mapped[mapping.Target] = resolvedId;
                        continue;
                    }

                    mapped[mapping.Target] = !string.IsNullOrEmpty(raw) ? raw : mapping.Default;
                }

                results.Add(new MappedRowResult { Values = mapped, Error = error });
            }

            return results;
        }
    }
}
