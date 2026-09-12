using System.Globalization;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Ingestion;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Ingestion.Writers
{
    public class CenterProductFieldUpdateIngestionWriter : IIngestionWriter
    {
        private static readonly HashSet<string> ProtectedFields = new(StringComparer.OrdinalIgnoreCase)
        {
            nameof(CenterProduct.Id), nameof(CenterProduct.IdProduct), nameof(CenterProduct.IdCenter), nameof(CenterProduct.Adu),
            "createdAt", "updatedAt", "deletedAt", "createdBy", "updatedBy", "deletedBy"
        };

        private static readonly Dictionary<string, PropertyInfo> UpdatableProperties = typeof(CenterProduct)
            .GetProperties()
            .Where(p => p.CanWrite && !ProtectedFields.Contains(p.Name) && IsSupportedType(p.PropertyType))
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public CenterProductFieldUpdateIngestionWriter(ApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public bool CanHandle(IngestionSourceConfig source) => string.Equals(source.Table, "CenterProducts", StringComparison.OrdinalIgnoreCase);

        public async Task<IngestionWriteResult> WriteAsync(IngestionSourceConfig source, List<Dictionary<string, string?>> mappedRows, CancellationToken cancellationToken = default)
        {
            if (!source.Key.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).SequenceEqual(new[] { "IdCenter", "IdProduct" }, StringComparer.OrdinalIgnoreCase))
                throw new NotSupportedException($"View '{source.View}' (table 'CenterProducts') must declare key [\"IdProduct\", \"IdCenter\"].");

            var result = new IngestionWriteResult();

            var updateTargets = mappedRows
                .SelectMany(r => r.Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(target => !source.Key.Contains(target, StringComparer.OrdinalIgnoreCase))
                .ToList();

            foreach (var target in updateTargets)
            {
                if (!UpdatableProperties.ContainsKey(target))
                    throw new NotSupportedException($"View '{source.View}' (table 'CenterProducts') maps field '{target}', which isn't an updatable CenterProduct field.");
            }

            var parsedRows = new List<(Dictionary<string, string?> Row, int IdProduct, int IdCenter)>();
            foreach (var row in mappedRows)
            {
                if (!int.TryParse(row.GetValueOrDefault("IdProduct"), out var idProduct) ||
                    !int.TryParse(row.GetValueOrDefault("IdCenter"), out var idCenter))
                {
                    result.Errors.Add("Row skipped: missing/invalid IdProduct or IdCenter.");
                    continue;
                }

                parsedRows.Add((row, idProduct, idCenter));
            }

            var productIds = parsedRows.Select(r => r.IdProduct).Distinct().ToList();
            var centerIds = parsedRows.Select(r => r.IdCenter).Distinct().ToList();

            var existing = await _context.CenterProduct
                .Where(cp => cp.deletedAt == null && productIds.Contains(cp.IdProduct) && centerIds.Contains(cp.IdCenter))
                .ToDictionaryAsync(cp => (cp.IdProduct, cp.IdCenter), cancellationToken);

            var now = DateTime.UtcNow;
            var userId = _currentUser.UserId;

            foreach (var (row, idProduct, idCenter) in parsedRows)
            {
                if (!existing.TryGetValue((idProduct, idCenter), out var centerProduct))
                {
                    result.Errors.Add($"Row skipped: no CenterProduct found for IdProduct={idProduct}, IdCenter={idCenter}.");
                    continue;
                }

                foreach (var target in updateTargets)
                {
                    if (!row.TryGetValue(target, out var rawValue))
                        continue;

                    UpdatableProperties[target].SetValue(centerProduct, ConvertValue(rawValue, UpdatableProperties[target].PropertyType));
                }

                centerProduct.updatedAt = now;
                centerProduct.updatedBy = userId;
                result.Updated++;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return result;
        }

        private static bool IsSupportedType(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;
            return underlying == typeof(string) || underlying == typeof(int) || underlying == typeof(decimal) ||
                   underlying == typeof(DateTime) || underlying == typeof(bool) || underlying.IsEnum;
        }

        private static object? ConvertValue(string? raw, Type propertyType)
        {
            var underlying = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            var acceptsNull = underlying != propertyType || underlying == typeof(string);

            if (string.IsNullOrWhiteSpace(raw))
                return acceptsNull ? null : throw new NotSupportedException($"A value is required for non-nullable field of type '{underlying.Name}'.");

            if (underlying == typeof(string)) return raw;
            if (underlying == typeof(int)) return int.Parse(raw, CultureInfo.InvariantCulture);
            if (underlying == typeof(decimal)) return decimal.Parse(raw, NumberStyles.Any, CultureInfo.InvariantCulture);
            if (underlying == typeof(DateTime)) return DateTime.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None);
            if (underlying == typeof(bool)) return bool.Parse(raw);
            if (underlying.IsEnum) return Enum.Parse(underlying, raw, ignoreCase: true);

            throw new NotSupportedException($"Unsupported field type '{underlying.Name}' for ingestion.");
        }
    }
}
