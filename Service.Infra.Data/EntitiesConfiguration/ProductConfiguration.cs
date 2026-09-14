using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Service.Domain.Entities;

namespace Service.Infra.Data.EntitiesConfiguration
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products");
            builder.ConfigureAuditFields();
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Reference).IsRequired().HasMaxLength(50);
            builder.Property(p => p.Description).IsRequired().HasMaxLength(200);
            builder.Property(p => p.AuxiliarMaterialCode).HasMaxLength(50);
            builder.Property(p => p.UnitOfMeasure).IsRequired().HasMaxLength(20);
            builder.Property(p => p.Weight).HasPrecision(18, 4);
            builder.Property(p => p.Volume).HasPrecision(18, 4);
            builder.Property(p => p.Barcode).HasMaxLength(50);
            builder.Property(p => p.Category).HasMaxLength(100);
            builder.Property(p => p.Segment).HasMaxLength(100);
            builder.Property(p => p.Value).HasPrecision(18, 4);
            builder.Property(p => p.Pallet).HasPrecision(18, 4);
            builder.Property(p => p.Line).HasMaxLength(100);
            builder.Property(p => p.Subline).HasMaxLength(100);
            builder.Property(p => p.Brand).HasMaxLength(100);
            builder.Property(p => p.WorkCenter).HasMaxLength(50);
        }
    }
}
