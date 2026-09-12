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
                var keyPrefix = DescribeKey(source, rawRow);

                foreach (var mapping in source.FieldMappings)
                {
                    var raw = mapping.Source != null && rawRow.TryGetValue(mapping.Source, out var value) ? value : null;

                    if (mapping.Lookup != null)
                    {
                        if (string.IsNullOrWhiteSpace(raw))
                        {
                            if (!mapping.Required)
                                continue;

                            error ??= $"{keyPrefix}missing value for lookup field '{mapping.Source}' (target '{mapping.Target}').";
                            continue;
                        }

                        if (!resolvedLookups.TryGetValue((mapping.Lookup.Entity, mapping.Lookup.By), out var lookupMap) ||
                            !lookupMap.TryGetValue(raw, out var resolvedId))
                        {
                            error ??= $"{keyPrefix}could not resolve {mapping.Lookup.Entity}.{mapping.Lookup.By} = '{raw}' (target '{mapping.Target}').";
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

        // Identifies a row for error messages using its configured "key" columns (the CSV source
        // columns the client can actually search for), instead of a line/row number.
        private static string DescribeKey(IngestionSourceConfig source, IReadOnlyDictionary<string, string?> rawRow)
        {
            if (source.Key.Count == 0)
                return string.Empty;

            var parts = new List<string>();
            foreach (var keyField in source.Key)
            {
                var mapping = source.FieldMappings.FirstOrDefault(f => string.Equals(f.Target, keyField, StringComparison.OrdinalIgnoreCase));
                if (mapping?.Source == null)
                    continue;

                rawRow.TryGetValue(mapping.Source, out var value);
                parts.Add($"{mapping.Source}={value}");
            }

            return parts.Count > 0 ? $"[{string.Join(", ", parts)}] " : string.Empty;
        }
    }
}
