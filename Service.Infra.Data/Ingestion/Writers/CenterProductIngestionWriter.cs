using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Ingestion;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Ingestion.Writers
{
    public class CenterProductIngestionWriter : IIngestionWriter
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public CenterProductIngestionWriter(ApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public bool CanHandle(IngestionSourceConfig source) => string.Equals(source.View, "CenterProducts", StringComparison.OrdinalIgnoreCase);

        public async Task<IngestionWriteResult> WriteAsync(IngestionSourceConfig source, List<Dictionary<string, string?>> mappedRows, CancellationToken cancellationToken = default)
        {
            var result = new IngestionWriteResult();
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
                .ToListAsync(cancellationToken);
            var existingMap = existing.ToDictionary(cp => (cp.IdProduct, cp.IdCenter), cp => cp);

            var now = DateTime.UtcNow;
            var userId = _currentUser.UserId;

            foreach (var (row, idProduct, idCenter) in parsedRows)
            {
                var idOriginCenter = TryParseInt(row.GetValueOrDefault("IdOriginCenter"));
                var idProvider = TryParseInt(row.GetValueOrDefault("IdProvider"));
                var packQuantity = TryParseDecimal(row.GetValueOrDefault("PackQuantity")) ?? 0;
                var moq = TryParseDecimal(row.GetValueOrDefault("Moq")) ?? 0;
                var leadTime = TryParseInt(row.GetValueOrDefault("LeadTime")) ?? 0;
                var frequency = TryParseInt(row.GetValueOrDefault("Frequency")) ?? 0;

                if (existingMap.TryGetValue((idProduct, idCenter), out var centerProduct))
                {
                    // HistoryAduDays/FutureAduDays are intentionally NOT updated here: they're a
                    // create-time default, and re-syncing from CSV shouldn't clobber a value the
                    // user later tuned via PUT (same reasoning as History.DiscardStatus).
                    // Stock is also NOT touched here: it's owned by the dedicated "Stock" ingestion
                    // view/writer, which always updates it independently.
                    centerProduct.IdOriginCenter = idOriginCenter;
                    centerProduct.IdProvider = idProvider;
                    centerProduct.PackQuantity = packQuantity;
                    centerProduct.Moq = moq;
                    centerProduct.LeadTime = leadTime;
                    centerProduct.Frequency = frequency;
                    centerProduct.updatedAt = now;
                    centerProduct.updatedBy = userId;
                    result.Updated++;
                }
                else
                {
                    var newCenterProduct = new CenterProduct
                    {
                        IdProduct = idProduct,
                        IdCenter = idCenter,
                        IdOriginCenter = idOriginCenter,
                        IdProvider = idProvider,
                        PackQuantity = packQuantity,
                        Moq = moq,
                        LeadTime = leadTime,
                        Frequency = frequency,
                        HistoryAduDays = TryParseInt(row.GetValueOrDefault("HistoryAduDays")),
                        FutureAduDays = TryParseInt(row.GetValueOrDefault("FutureAduDays")),
                        createdAt = now,
                        createdBy = userId
                    };
                    _context.CenterProduct.Add(newCenterProduct);
                    existingMap[(idProduct, idCenter)] = newCenterProduct;
                    result.Inserted++;
                }
            }

            if (source.DeleteNonSent)
            {
                var sentKeys = parsedRows.Select(r => (r.IdProduct, r.IdCenter)).ToHashSet();
                var toDelete = existing.Where(cp => !sentKeys.Contains((cp.IdProduct, cp.IdCenter))).ToList();

                foreach (var centerProduct in toDelete)
                {
                    centerProduct.deletedAt = now;
                    centerProduct.deletedBy = userId;
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
    }
}
