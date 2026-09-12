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

        public bool CanHandle(string view) => string.Equals(view, "CenterProducts", StringComparison.OrdinalIgnoreCase);

        public async Task<IngestionWriteResult> WriteAsync(List<Dictionary<string, string?>> mappedRows, bool deleteNonSent, CancellationToken cancellationToken = default)
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
                var packQuantity = TryParseDecimal(row.GetValueOrDefault("PackQuantity")) ?? 0;
                var moq = TryParseDecimal(row.GetValueOrDefault("Moq")) ?? 0;
                var leadTime = TryParseInt(row.GetValueOrDefault("LeadTime")) ?? 0;
                var frequency = TryParseInt(row.GetValueOrDefault("Frequency")) ?? 0;
                var stock = TryParseDecimal(row.GetValueOrDefault("Stock")) ?? 0;

                if (existingMap.TryGetValue((idProduct, idCenter), out var centerProduct))
                {
                    centerProduct.PackQuantity = packQuantity;
                    centerProduct.Moq = moq;
                    centerProduct.LeadTime = leadTime;
                    centerProduct.Frequency = frequency;
                    centerProduct.Stock = stock;
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
                        PackQuantity = packQuantity,
                        Moq = moq,
                        LeadTime = leadTime,
                        Frequency = frequency,
                        Stock = stock,
                        createdAt = now,
                        createdBy = userId
                    };
                    _context.CenterProduct.Add(newCenterProduct);
                    existingMap[(idProduct, idCenter)] = newCenterProduct;
                    result.Inserted++;
                }
            }

            if (deleteNonSent)
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
