using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Enums;
using Service.Domain.Ingestion;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Ingestion.Writers
{
    // Dispatched by Table (not View), same as the generic writer, so every OrderType view
    // (ProductionOrder/PurchaseOrder/SaleOrder/Transfers/...) shares this one class via config alone —
    // but it's a dedicated, hand-written writer (not GenericTableIngestionWriter) because Orders needs
    // business logic no generic engine should have: see the Type-scoped, fictional-excluding
    // delete-non-sent behavior below.
    public class OrderIngestionWriter : IIngestionWriter
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public OrderIngestionWriter(ApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public bool CanHandle(IngestionSourceConfig source) => string.Equals(source.Table, "Orders", StringComparison.OrdinalIgnoreCase);

        public async Task<IngestionWriteResult> WriteAsync(IngestionSourceConfig source, List<Dictionary<string, string?>> mappedRows, CancellationToken cancellationToken = default)
        {
            var result = new IngestionWriteResult();
            var parsedRows = new List<(Dictionary<string, string?> Row, string OrderNumber, int? IdDestinyCenter, int? IdOriginCenter, int IdProduct, OrderType Type)>();

            foreach (var row in mappedRows)
            {
                var orderNumber = row.GetValueOrDefault("OrderNumber");
                if (string.IsNullOrEmpty(orderNumber))
                {
                    result.Errors.Add("Row skipped: missing required field 'OrderNumber'.");
                    continue;
                }

                if (!int.TryParse(row.GetValueOrDefault("IdProduct"), out var idProduct))
                {
                    result.Errors.Add($"Row skipped for OrderNumber '{orderNumber}': missing/invalid IdProduct.");
                    continue;
                }

                if (!Enum.TryParse<OrderType>(row.GetValueOrDefault("Type"), ignoreCase: true, out var type))
                {
                    result.Errors.Add($"Row skipped for OrderNumber '{orderNumber}': missing/invalid Type.");
                    continue;
                }

                var idDestinyCenter = TryParseInt(row.GetValueOrDefault("IdDestinyCenter"));
                var idOriginCenter = TryParseInt(row.GetValueOrDefault("IdOriginCenter"));

                parsedRows.Add((row, orderNumber, idDestinyCenter, idOriginCenter, idProduct, type));
            }

            var orderNumbers = parsedRows.Select(r => r.OrderNumber).Distinct().ToList();
            var sentTypes = parsedRows.Select(r => r.Type).Distinct().ToList();

            // Fictional Orders are invisible to ingestion entirely — never matched, never updated, never
            // deleted, even when their key collides with a sent row. Also scoped by Type: a row from one
            // OrderType feed must never match/update an Order that actually belongs to a different type.
            var matchCandidates = await _context.Order
                .Where(o => o.deletedAt == null && !o.IsFictional && sentTypes.Contains(o.Type) && orderNumbers.Contains(o.OrderNumber))
                .ToListAsync(cancellationToken);
            var existingMap = matchCandidates.ToDictionary(
                o => (o.OrderNumber, IdDestinyCenter: o.IdDestinyCenter ?? 0, IdOriginCenter: o.IdOriginCenter ?? 0, o.IdProduct),
                o => o);

            var now = DateTime.UtcNow;
            var userId = _currentUser.UserId;

            foreach (var (row, orderNumber, idDestinyCenter, idOriginCenter, idProduct, type) in parsedRows)
            {
                var measurementUnit = row.GetValueOrDefault("MeasurementUnit");

                if (!decimal.TryParse(row.GetValueOrDefault("Quantity"), NumberStyles.Any, CultureInfo.InvariantCulture, out var quantity) ||
                    string.IsNullOrEmpty(measurementUnit) ||
                    !DateTime.TryParse(row.GetValueOrDefault("CreationDate"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var creationDate) ||
                    !bool.TryParse(row.GetValueOrDefault("IsInbound"), out var isInbound) ||
                    !bool.TryParse(row.GetValueOrDefault("IsOutbound"), out var isOutbound))
                {
                    result.Errors.Add($"Row skipped for OrderNumber '{orderNumber}': missing/invalid Quantity, MeasurementUnit, CreationDate, IsInbound or IsOutbound.");
                    continue;
                }

                var deliveredQuantity = TryParseDecimal(row.GetValueOrDefault("DeliveredQuantity")) ?? 0;
                var idPartner = TryParseInt(row.GetValueOrDefault("IdPartner"));
                var position = TryParseInt(row.GetValueOrDefault("Position"));
                var deliveryDate = TryParseUtcDateTime(row.GetValueOrDefault("DeliveryDate"));
                var notes = row.GetValueOrDefault("Notes");
                var key = (orderNumber, IdDestinyCenter: idDestinyCenter ?? 0, IdOriginCenter: idOriginCenter ?? 0, idProduct);

                if (existingMap.TryGetValue(key, out var order))
                {
                    order.IdPartner = idPartner;
                    order.Quantity = quantity;
                    order.DeliveredQuantity = deliveredQuantity;
                    order.MeasurementUnit = measurementUnit;
                    order.Position = position;
                    order.CreationDate = DateTime.SpecifyKind(creationDate, DateTimeKind.Utc);
                    order.DeliveryDate = deliveryDate;
                    order.Notes = notes;
                    order.updatedAt = now;
                    order.updatedBy = userId;
                    result.Updated++;
                }
                else
                {
                    var newOrder = new Order
                    {
                        OrderNumber = orderNumber,
                        IdPartner = idPartner,
                        IdDestinyCenter = idDestinyCenter,
                        IdOriginCenter = idOriginCenter,
                        IdProduct = idProduct,
                        Quantity = quantity,
                        DeliveredQuantity = deliveredQuantity,
                        MeasurementUnit = measurementUnit,
                        Position = position,
                        CreationDate = DateTime.SpecifyKind(creationDate, DateTimeKind.Utc),
                        DeliveryDate = deliveryDate,
                        Notes = notes,
                        Type = type,
                        IsInbound = isInbound,
                        IsOutbound = isOutbound,
                        IsFictional = false,
                        createdAt = now,
                        createdBy = userId
                    };
                    _context.Order.Add(newOrder);
                    existingMap[key] = newOrder;
                    result.Inserted++;
                }
            }

            // Always soft-delete non-fictional Orders of the same Type(s) present in this file that
            // weren't sent — this ignores IngestionSourceConfig.DeleteNonSent entirely (unlike every
            // other writer) because it's inherently safe: scoped to exactly the Type(s) in this file, so
            // e.g. running Transfers can never touch PurchaseOrder/SaleOrder/ProductionOrder rows, and
            // fictional Orders are excluded from the candidate set altogether.
            if (sentTypes.Count > 0)
            {
                var sentKeys = parsedRows
                    .Select(r => (r.OrderNumber, IdDestinyCenter: r.IdDestinyCenter ?? 0, IdOriginCenter: r.IdOriginCenter ?? 0, r.IdProduct))
                    .ToHashSet();

                var deletionCandidates = await _context.Order
                    .Where(o => o.deletedAt == null && !o.IsFictional && sentTypes.Contains(o.Type))
                    .ToListAsync(cancellationToken);

                var toDelete = deletionCandidates
                    .Where(o => !sentKeys.Contains((o.OrderNumber, o.IdDestinyCenter ?? 0, o.IdOriginCenter ?? 0, o.IdProduct)))
                    .ToList();

                foreach (var order in toDelete)
                {
                    order.deletedAt = now;
                    order.deletedBy = userId;
                }
                result.Deleted = toDelete.Count;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return result;
        }

        private static decimal? TryParseDecimal(string? value) =>
            decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

        private static int? TryParseInt(string? value) =>
            int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

        private static DateTime? TryParseUtcDateTime(string? value) =>
            DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
                ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                : null;
    }
}
