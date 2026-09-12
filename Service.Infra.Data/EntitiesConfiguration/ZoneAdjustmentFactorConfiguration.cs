using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Service.Domain.Entities;

namespace Service.Infra.Data.EntitiesConfiguration
{
    public class ZoneAdjustmentFactorConfiguration : IEntityTypeConfiguration<ZoneAdjustmentFactor>
    {
        public void Configure(EntityTypeBuilder<ZoneAdjustmentFactor> builder)
        {
            builder.ToTable("ZoneAdjustmentFactors");
            builder.ConfigureAuditFields();
            builder.HasKey(z => z.Id);

            builder.Property(z => z.IdProduct).IsRequired();
            builder.Property(z => z.IdCenter).IsRequired();
            builder.Property(z => z.TargetZone).IsRequired();
            builder.Property(z => z.AdjustmentType).IsRequired();
            builder.Property(z => z.AdjustmentValue).IsRequired().HasPrecision(18, 4);
            builder.Property(z => z.Obs).HasMaxLength(300);
            builder.Property(z => z.IsActive).IsRequired();
            builder.Property(z => z.EffectiveFrom).IsRequired();
            builder.Property(z => z.EffectiveTo).IsRequired();

            builder.HasOne(z => z.Product)
                .WithMany()
                .HasForeignKey(z => z.IdProduct)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(z => z.Center)
                .WithMany()
                .HasForeignKey(z => z.IdCenter)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
