using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Service.Domain.Entities;

namespace Service.Infra.Data.EntitiesConfiguration
{
    public class DemandAdjustmentFactorConfiguration : IEntityTypeConfiguration<DemandAdjustmentFactor>
    {
        public void Configure(EntityTypeBuilder<DemandAdjustmentFactor> builder)
        {
            builder.ToTable("DemandAdjustmentFactors");
            builder.ConfigureAuditFields();
            builder.HasKey(d => d.Id);

            builder.Property(d => d.IdProduct).IsRequired();
            builder.Property(d => d.IdCenter).IsRequired();
            builder.Property(d => d.EffectiveFrom).IsRequired();
            builder.Property(d => d.EffectiveTo).IsRequired();
            builder.Property(d => d.IsActive).IsRequired();
            builder.Property(d => d.Obs).HasMaxLength(300);
            builder.Property(d => d.AdjustmentType).IsRequired();
            builder.Property(d => d.AdjustmentValue).IsRequired().HasPrecision(18, 4);

            builder.HasOne(d => d.Product)
                .WithMany()
                .HasForeignKey(d => d.IdProduct)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(d => d.Center)
                .WithMany()
                .HasForeignKey(d => d.IdCenter)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
