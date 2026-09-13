using System.Collections;
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
    // Upserts an arbitrary table by config alone (Table + Key + FieldMappings), with no per-table
    // C# class needed. Adding a table here (one line) is the only code change ever required — a new
    // view targeting an already-allowed table is pure JSON. See CLAUDE.md for the full rationale.
    public class GenericTableIngestionWriter : IIngestionWriter
    {
        // Plain-ASCII markers for joining composite-key field values into one dictionary-lookup
        // string. Not control characters on purpose (editor/tooling-safe) — collision with real
        // business key data (order numbers, codes, references) is not a realistic concern.
        private const string KeyPartSeparator = "::";
        private const string NullKeyPart = "<null>";

        // "Orders" is deliberately NOT here — it needs its own OrderIngestionWriter (Type-scoped,
        // fictional-excluding delete-non-sent business logic no generic engine should have).
        private static readonly HashSet<string> AllowedTables = new(StringComparer.OrdinalIgnoreCase)
        {
            "CenterProducts"
        };

        private static readonly HashSet<string> AlwaysExcludedProperties = new(StringComparer.OrdinalIgnoreCase)
        {
            nameof(BaseEntity.Id), "createdAt", "updatedAt", "deletedAt", "createdBy", "updatedBy", "deletedBy"
        };

        private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> PropertyCache = [];

        private static readonly MethodInfo FetchExistingGenericMethod = typeof(GenericTableIngestionWriter)
            .GetMethod(nameof(FetchExistingGenericAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;

        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GenericTableIngestionWriter(ApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public bool CanHandle(IngestionSourceConfig source) =>
            !string.IsNullOrWhiteSpace(source.Table) &&
            AllowedTables.Contains(source.Table) &&
            ResolveEntityType(source.Table) != null;

        public async Task<IngestionWriteResult> WriteAsync(IngestionSourceConfig source, List<Dictionary<string, string?>> mappedRows, CancellationToken cancellationToken = default)
        {
            var entityType = ResolveEntityType(source.Table!)
                ?? throw new NotSupportedException($"No entity mapped to table '{source.Table}'.");

            var properties = GetUpdatableProperties(entityType);

            if (source.Key.Count == 0)
                throw new NotSupportedException($"View '{source.View}' (table '{source.Table}') must declare at least one key field.");

            foreach (var keyField in source.Key)
            {
                if (!properties.ContainsKey(keyField))
                    throw new NotSupportedException($"View '{source.View}' (table '{source.Table}') declares '{keyField}' as key, but it isn't a recognized field for that table.");
            }

            var mappedTargets = mappedRows.SelectMany(r => r.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            foreach (var target in mappedTargets)
            {
                if (!properties.ContainsKey(target))
                    throw new NotSupportedException($"View '{source.View}' (table '{source.Table}') maps field '{target}', which isn't an updatable field for that table.");
            }

            var result = new IngestionWriteResult();
            var parsedRows = new List<(Dictionary<string, string?> Row, string KeyString, string KeyDisplay)>();

            foreach (var row in mappedRows)
            {
                if (!TryBuildKeyString(row, source.Key, properties, out var keyString))
                {
                    result.Errors.Add($"Row skipped: missing/invalid value for key field(s) [{string.Join(", ", source.Key)}].");
                    continue;
                }

                var keyDisplay = string.Join(", ", source.Key.Select(k => $"{k}={row.GetValueOrDefault(k)}"));
                parsedRows.Add((row, keyString, keyDisplay));
            }

            var existing = await FetchExistingAsync(entityType, cancellationToken);
            var existingMap = existing.ToDictionary(e => BuildEntityKeyString(e, source.Key, properties), e => e);

            var now = DateTime.UtcNow;
            var userId = _currentUser.UserId;
            var sentKeys = new HashSet<string>();

            foreach (var (row, keyString, keyDisplay) in parsedRows)
            {
                sentKeys.Add(keyString);

                var isUpdate = existingMap.TryGetValue(keyString, out var entity);
                var targetsToSet = isUpdate
                    ? mappedTargets.Where(t => !source.Key.Contains(t, StringComparer.OrdinalIgnoreCase)).ToList()
                    : mappedTargets;

                // Convert every value up front, before touching the entity — a single bad value
                // must fail the whole row, not leave it partially mutated in the change tracker.
                var converted = new List<(PropertyInfo Property, object? Value)>();
                string? conversionError = null;

                foreach (var target in targetsToSet)
                {
                    if (!row.TryGetValue(target, out var rawValue))
                        continue;

                    try
                    {
                        converted.Add((properties[target], ConvertValue(rawValue, properties[target].PropertyType)));
                    }
                    catch (Exception ex)
                    {
                        conversionError = $"Row skipped for key [{keyDisplay}]: invalid value for field '{target}' ({ex.Message}).";
                        break;
                    }
                }

                if (conversionError != null)
                {
                    result.Errors.Add(conversionError);
                    continue;
                }

                if (isUpdate)
                {
                    foreach (var (property, value) in converted)
                        property.SetValue(entity, value);

                    entity!.updatedAt = now;
                    entity.updatedBy = userId;
                    result.Updated++;
                }
                else
                {
                    if (!source.AllowInsert)
                    {
                        result.Errors.Add($"Row skipped: no existing '{source.Table}' row found for key [{keyDisplay}], and this view doesn't create new rows.");
                        continue;
                    }

                    var newEntity = (BaseEntity)Activator.CreateInstance(entityType)!;
                    foreach (var (property, value) in converted)
                        property.SetValue(newEntity, value);

                    newEntity.createdAt = now;
                    newEntity.createdBy = userId;
                    _context.Add(newEntity);
                    existingMap[keyString] = newEntity;
                    result.Inserted++;
                }
            }

            if (source.DeleteNonSent)
            {
                var toDelete = existing.Where(e => !sentKeys.Contains(BuildEntityKeyString(e, source.Key, properties))).ToList();
                foreach (var entity in toDelete)
                {
                    entity.deletedAt = now;
                    entity.deletedBy = userId;
                }
                result.Deleted = toDelete.Count;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return result;
        }

        private Type? ResolveEntityType(string tableName) =>
            _context.Model.GetEntityTypes()
                .FirstOrDefault(e => string.Equals(e.GetTableName(), tableName, StringComparison.OrdinalIgnoreCase))
                ?.ClrType;

        private static Dictionary<string, PropertyInfo> GetUpdatableProperties(Type entityType)
        {
            if (PropertyCache.TryGetValue(entityType, out var cached))
                return cached;

            var properties = entityType.GetProperties()
                .Where(p => p.CanWrite && !AlwaysExcludedProperties.Contains(p.Name) && IsSupportedType(p.PropertyType))
                .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

            PropertyCache[entityType] = properties;
            return properties;
        }

        private static bool TryBuildKeyString(Dictionary<string, string?> row, List<string> key, Dictionary<string, PropertyInfo> properties, out string keyString)
        {
            var parts = new List<string>();

            foreach (var keyField in key)
            {
                var raw = row.GetValueOrDefault(keyField);
                if (string.IsNullOrWhiteSpace(raw))
                {
                    keyString = string.Empty;
                    return false;
                }

                object? converted;
                try
                {
                    converted = ConvertValue(raw, properties[keyField].PropertyType);
                }
                catch
                {
                    keyString = string.Empty;
                    return false;
                }

                parts.Add(converted?.ToString() ?? NullKeyPart);
            }

            keyString = string.Join(KeyPartSeparator, parts);
            return true;
        }

        private static string BuildEntityKeyString(BaseEntity entity, List<string> key, Dictionary<string, PropertyInfo> properties) =>
            string.Join(KeyPartSeparator, key.Select(k => properties[k].GetValue(entity)?.ToString() ?? NullKeyPart));

        private async Task<List<BaseEntity>> FetchExistingAsync(Type entityType, CancellationToken cancellationToken)
        {
            var task = (Task)FetchExistingGenericMethod.MakeGenericMethod(entityType).Invoke(this, [cancellationToken])!;
            await task.ConfigureAwait(false);
            var typedList = (IEnumerable)task.GetType().GetProperty(nameof(Task<object>.Result))!.GetValue(task)!;
            return typedList.Cast<BaseEntity>().ToList();
        }

        private async Task<List<TEntity>> FetchExistingGenericAsync<TEntity>(CancellationToken cancellationToken) where TEntity : BaseEntity =>
            await _context.Set<TEntity>().Where(e => e.deletedAt == null).ToListAsync(cancellationToken);

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
            if (underlying == typeof(DateTime)) return DateTime.SpecifyKind(DateTime.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None), DateTimeKind.Utc);
            if (underlying == typeof(bool)) return bool.Parse(raw);
            if (underlying.IsEnum) return Enum.Parse(underlying, raw, ignoreCase: true);

            throw new NotSupportedException($"Unsupported field type '{underlying.Name}' for ingestion.");
        }
    }
}
