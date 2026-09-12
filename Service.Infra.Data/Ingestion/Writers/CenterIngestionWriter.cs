using Microsoft.EntityFrameworkCore;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Ingestion;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Ingestion.Writers
{
    public class CenterIngestionWriter : IIngestionWriter
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public CenterIngestionWriter(ApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public bool CanHandle(string view) => string.Equals(view, "Centers", StringComparison.OrdinalIgnoreCase);

        public async Task<IngestionWriteResult> WriteAsync(List<Dictionary<string, string?>> mappedRows, bool deleteNonSent, CancellationToken cancellationToken = default)
        {
            var result = new IngestionWriteResult();

            var codes = mappedRows
                .Select(r => r.GetValueOrDefault("Code"))
                .Where(c => !string.IsNullOrEmpty(c))
                .Select(c => c!)
                .Distinct()
                .ToList();

            var existing = await _context.Center
                .Where(c => c.deletedAt == null && codes.Contains(c.Code))
                .ToDictionaryAsync(c => c.Code, c => c, cancellationToken);

            var now = DateTime.UtcNow;
            var userId = _currentUser.UserId;

            foreach (var row in mappedRows)
            {
                var code = row.GetValueOrDefault("Code");
                if (string.IsNullOrEmpty(code))
                {
                    result.Errors.Add("Row skipped: missing required field 'Code'.");
                    continue;
                }

                var description = row.GetValueOrDefault("Description");
                if (string.IsNullOrEmpty(description))
                {
                    result.Errors.Add($"Row skipped for Code '{code}': missing required field 'Description'.");
                    continue;
                }

                if (existing.TryGetValue(code, out var center))
                {
                    center.Description = description;
                    center.City = row.GetValueOrDefault("City");
                    center.Zone = row.GetValueOrDefault("Zone");
                    center.updatedAt = now;
                    center.updatedBy = userId;
                    result.Updated++;
                }
                else
                {
                    var newCenter = new Center
                    {
                        Code = code,
                        Description = description,
                        City = row.GetValueOrDefault("City"),
                        Zone = row.GetValueOrDefault("Zone"),
                        createdAt = now,
                        createdBy = userId
                    };
                    _context.Center.Add(newCenter);
                    existing[code] = newCenter;
                    result.Inserted++;
                }
            }

            if (deleteNonSent)
            {
                var toDelete = await _context.Center
                    .Where(c => c.deletedAt == null && !codes.Contains(c.Code))
                    .ToListAsync(cancellationToken);

                foreach (var center in toDelete)
                {
                    center.deletedAt = now;
                    center.deletedBy = userId;
                }
                result.Deleted = toDelete.Count;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return result;
        }
    }
}
