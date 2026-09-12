using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Service.Domain.Entities;

namespace Service.Infra.Data.EntitiesConfiguration
{
    public class BufferAdjustmentFactorConfiguration : IEntityTypeConfiguration<BufferAdjustmentFactor>
    {
        public void Configure(EntityTypeBuilder<BufferAdjustmentFactor> builder)
        {
            builder.ToTable("BufferAdjustmentFactors");
            builder.ConfigureAuditFields();
            builder.HasKey(b => b.Id);

            builder.Property(b => b.IdProduct).IsRequired();
            builder.Property(b => b.IdCenter).IsRequired();
            builder.Property(b => b.EffectiveFrom).IsRequired();
            builder.Property(b => b.EffectiveTo).IsRequired();
            builder.Property(b => b.BufferType).IsRequired();
            builder.Property(b => b.BufferDdmrpRed).IsRequired().HasPrecision(18, 4);
            builder.Property(b => b.BufferDdmrpYellow).IsRequired().HasPrecision(18, 4);
            builder.Property(b => b.BufferDdmrpGreen).IsRequired().HasPrecision(18, 4);
            builder.Property(b => b.Obs).HasMaxLength(300);
            builder.Property(b => b.IsActive).IsRequired();
            builder.Property(b => b.BufferTypeOld);
            builder.Property(b => b.BufferDdmrpRedOld).HasPrecision(18, 4);
            builder.Property(b => b.BufferDdmrpYellowOld).HasPrecision(18, 4);
            builder.Property(b => b.BufferDdmrpGreenOld).HasPrecision(18, 4);

            builder.HasOne(b => b.Product)
                .WithMany()
                .HasForeignKey(b => b.IdProduct)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(b => b.Center)
                .WithMany()
                .HasForeignKey(b => b.IdCenter)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
