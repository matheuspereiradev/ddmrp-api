using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Ingestion;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Ingestion.Writers
{
    public class ProductIngestionWriter : IIngestionWriter
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public ProductIngestionWriter(ApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public bool CanHandle(IngestionSourceConfig source) => string.Equals(source.View, "Products", StringComparison.OrdinalIgnoreCase);

        public async Task<IngestionWriteResult> WriteAsync(IngestionSourceConfig source, List<Dictionary<string, string?>> mappedRows, CancellationToken cancellationToken = default)
        {
            var result = new IngestionWriteResult();

            var references = mappedRows
                .Select(r => r.GetValueOrDefault("Reference"))
                .Where(r => !string.IsNullOrEmpty(r))
                .Select(r => r!)
                .Distinct()
                .ToList();

            var existing = await _context.Product
                .Where(p => p.deletedAt == null && references.Contains(p.Reference))
                .ToDictionaryAsync(p => p.Reference, p => p, cancellationToken);

            var now = DateTime.UtcNow;
            var userId = _currentUser.UserId;

            foreach (var row in mappedRows)
            {
                var reference = row.GetValueOrDefault("Reference");
                if (string.IsNullOrEmpty(reference))
                {
                    result.Errors.Add("Row skipped: missing required field 'Reference'.");
                    continue;
                }

                var description = row.GetValueOrDefault("Description");
                var unitOfMeasure = row.GetValueOrDefault("UnitOfMeasure");
                if (string.IsNullOrEmpty(description) || string.IsNullOrEmpty(unitOfMeasure))
                {
                    result.Errors.Add($"Row skipped for Reference '{reference}': missing required field 'Description' or 'UnitOfMeasure'.");
                    continue;
                }

                if (existing.TryGetValue(reference, out var product))
                {
                    ApplyFields(product, row, description, unitOfMeasure);
                    product.updatedAt = now;
                    product.updatedBy = userId;
                    result.Updated++;
                }
                else
                {
                    var newProduct = new Product { Reference = reference, Description = description, UnitOfMeasure = unitOfMeasure };
                    ApplyFields(newProduct, row, description, unitOfMeasure);
                    newProduct.createdAt = now;
                    newProduct.createdBy = userId;
                    _context.Product.Add(newProduct);
                    existing[reference] = newProduct;
                    result.Inserted++;
                }
            }

            if (source.DeleteNonSent)
            {
                var toDelete = await _context.Product
                    .Where(p => p.deletedAt == null && !references.Contains(p.Reference))
                    .ToListAsync(cancellationToken);

                foreach (var product in toDelete)
                {
                    product.deletedAt = now;
                    product.deletedBy = userId;
                }
                result.Deleted = toDelete.Count;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return result;
        }

        private static void ApplyFields(Product product, Dictionary<string, string?> row, string description, string unitOfMeasure)
        {
            product.Description = description;
            product.UnitOfMeasure = unitOfMeasure;
            product.AuxiliarMaterialCode = row.GetValueOrDefault("AuxiliarMaterialCode");
            product.Weight = TryParseDecimal(row.GetValueOrDefault("Weight"));
            product.Volume = TryParseDecimal(row.GetValueOrDefault("Volume"));
            product.Barcode = row.GetValueOrDefault("Barcode");
            product.Category = row.GetValueOrDefault("Category");
            product.Segment = row.GetValueOrDefault("Segment");
            product.Value = TryParseDecimal(row.GetValueOrDefault("Value"));
            product.Pallet = TryParseDecimal(row.GetValueOrDefault("Pallet"));
            product.Line = row.GetValueOrDefault("Line");
            product.Subline = row.GetValueOrDefault("Subline");
            product.Brand = row.GetValueOrDefault("Brand");
            product.WorkCenter = row.GetValueOrDefault("WorkCenter");
        }

        private static decimal? TryParseDecimal(string? value) =>
            decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }
}
