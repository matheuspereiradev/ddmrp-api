using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Ingestion;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Ingestion.Writers
{
    public class ForecastIngestionWriter : IIngestionWriter
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public ForecastIngestionWriter(ApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public bool CanHandle(IngestionSourceConfig source) => string.Equals(source.View, "Forecast", StringComparison.OrdinalIgnoreCase);

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

            var existing = await _context.Forecast
                .Where(f => f.deletedAt == null && productIds.Contains(f.IdProduct) && centerIds.Contains(f.IdCenter))
                .ToListAsync(cancellationToken);
            var existingMap = existing.ToDictionary(f => (f.IdProduct, f.IdCenter, f.Date), f => f);

            var now = DateTime.UtcNow;
            var userId = _currentUser.UserId;

            foreach (var (idProduct, idCenter, date, quantity) in parsedRows)
            {
                if (existingMap.TryGetValue((idProduct, idCenter, date), out var forecast))
                {
                    forecast.Quantity = quantity;
                    forecast.updatedAt = now;
                    forecast.updatedBy = userId;
                    result.Updated++;
                }
                else
                {
                    var newForecast = new Forecast
                    {
                        IdProduct = idProduct,
                        IdCenter = idCenter,
                        Date = date,
                        Quantity = quantity,
                        createdAt = now,
                        createdBy = userId
                    };
                    _context.Forecast.Add(newForecast);
                    existingMap[(idProduct, idCenter, date)] = newForecast;
                    result.Inserted++;
                }
            }

            if (source.DeleteNonSent)
            {
                var sentKeys = parsedRows.Select(r => (r.IdProduct, r.IdCenter, r.Date)).ToHashSet();
                var toDelete = existing.Where(f => !sentKeys.Contains((f.IdProduct, f.IdCenter, f.Date))).ToList();

                foreach (var forecast in toDelete)
                {
                    forecast.deletedAt = now;
                    forecast.deletedBy = userId;
                }
                result.Deleted = toDelete.Count;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return result;
        }
    }
}
