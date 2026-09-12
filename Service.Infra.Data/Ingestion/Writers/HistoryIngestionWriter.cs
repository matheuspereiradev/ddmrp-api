using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Ingestion;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Ingestion.Writers
{
    public class HistoryIngestionWriter : IIngestionWriter
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public HistoryIngestionWriter(ApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public bool CanHandle(IngestionSourceConfig source) => string.Equals(source.View, "History", StringComparison.OrdinalIgnoreCase);

        public async Task<IngestionWriteResult> WriteAsync(IngestionSourceConfig source, List<Dictionary<string, string?>> mappedRows, CancellationToken cancellationToken = default)
        {
            var result = new IngestionWriteResult();
            var parsedRows = new List<(int IdProduct, int IdCenter, DateTime Date, decimal Quantity)>();

            foreach (var row in mappedRows)
            {
                if (!int.TryParse(row.GetValueOrDefault("IdProduct"), out var idProduct) ||
                    !int.TryParse(row.GetValueOrDefault("IdCenter"), out var idCenter) ||
                    !DateTime.TryParse(row.GetValueOrDefault("Date"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
                    !decimal.TryParse(row.GetValueOrDefault("Quantity"), NumberStyles.Any, CultureInfo.InvariantCulture, out var quantity))
                {
                    result.Errors.Add("Row skipped: missing/invalid IdProduct, IdCenter, Date or Quantity.");
                    continue;
                }

                parsedRows.Add((idProduct, idCenter, DateTime.SpecifyKind(date.Date, DateTimeKind.Utc), quantity));
            }

            var productIds = parsedRows.Select(r => r.IdProduct).Distinct().ToList();
            var centerIds = parsedRows.Select(r => r.IdCenter).Distinct().ToList();

            var existing = await _context.History
                .Where(h => h.deletedAt == null && productIds.Contains(h.IdProduct) && centerIds.Contains(h.IdCenter))
                .ToListAsync(cancellationToken);
            var existingMap = existing.ToDictionary(h => (h.IdProduct, h.IdCenter, h.Date), h => h);

            var now = DateTime.UtcNow;
            var userId = _currentUser.UserId;

            foreach (var (idProduct, idCenter, date, quantity) in parsedRows)
            {
                if (existingMap.TryGetValue((idProduct, idCenter, date), out var history))
                {
                    history.Quantity = quantity;
                    history.updatedAt = now;
                    history.updatedBy = userId;
                    result.Updated++;
                }
                else
                {
                    var newHistory = new History
                    {
                        IdProduct = idProduct,
                        IdCenter = idCenter,
                        Date = date,
                        Quantity = quantity,
                        createdAt = now,
                        createdBy = userId
                    };
                    _context.History.Add(newHistory);
                    existingMap[(idProduct, idCenter, date)] = newHistory;
                    result.Inserted++;
                }
            }

            if (source.DeleteNonSent)
            {
                var sentKeys = parsedRows.Select(r => (r.IdProduct, r.IdCenter, r.Date)).ToHashSet();
                var toDelete = existing.Where(h => !sentKeys.Contains((h.IdProduct, h.IdCenter, h.Date))).ToList();

                foreach (var history in toDelete)
                {
                    history.deletedAt = now;
                    history.deletedBy = userId;
                }
                result.Deleted = toDelete.Count;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return result;
        }
    }
}
