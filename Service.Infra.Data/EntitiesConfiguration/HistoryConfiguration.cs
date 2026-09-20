using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Service.Domain.Entities;
using Service.Domain.Enums;

namespace Service.Infra.Data.EntitiesConfiguration
{
    public class HistoryConfiguration : IEntityTypeConfiguration<History>
    {
        public void Configure(EntityTypeBuilder<History> builder)
        {
            builder.ToTable("Histories");
            builder.ConfigureAuditFields();
            builder.HasKey(h => h.Id);

            builder.Property(h => h.IdProduct).IsRequired();
            builder.Property(h => h.IdCenter).IsRequired();
            builder.Property(h => h.Consumption).IsRequired().HasPrecision(18, 4);
            builder.Property(h => h.Date).IsRequired();
            builder.Property(h => h.DiscardStatus).IsRequired().HasDefaultValue(DiscardStatus.NotReviewed);
            builder.Property(h => h.Stock).HasPrecision(18, 4);
            builder.Property(h => h.ReservedStock).HasPrecision(18, 4);
            builder.Ignore(h => h.AvailableStock);
            builder.Property(h => h.QualifiedDemand).HasPrecision(18, 4);
            builder.Property(h => h.OpenInbounds).HasPrecision(18, 4);
            builder.Property(h => h.OpenOutbound).HasPrecision(18, 4);
            builder.Property(h => h.Adu).HasPrecision(18, 4);
            builder.Property(h => h.RedSafeZone).HasPrecision(18, 4);
            builder.Property(h => h.RedBaseZone).HasPrecision(18, 4);
            builder.Property(h => h.YellowZone).HasPrecision(18, 4);
            builder.Property(h => h.GreenZone).HasPrecision(18, 4);
            builder.Property(h => h.PackQuantity).HasPrecision(18, 4);
            builder.Property(h => h.Moq).HasPrecision(18, 4);
            builder.Property(h => h.LeadTime);
            builder.Property(h => h.Frequency);
            builder.Property(h => h.IdTag);
            builder.Property(h => h.IdReason);
            builder.Property(h => h.StandardDeviation).HasPrecision(18, 4);
            builder.HasOne(h => h.BufferProfile)
                .WithMany()
                .HasForeignKey(h => h.IdBufferProfile)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Property(h => h.Cv).HasPrecision(18, 4);
            builder.Property(h => h.FutureAduDays);
            builder.Property(h => h.HistoryAduDays);
            builder.Property(h => h.Adi).HasPrecision(18, 4);
            builder.Property(h => h.ZafRedZone).HasPrecision(18, 4);
            builder.Property(h => h.ZafYellowZone).HasPrecision(18, 4);
            builder.Property(h => h.ZafGreenZone).HasPrecision(18, 4);
            builder.Ignore(h => h.StockDays);
            builder.Ignore(h => h.StockTotal);

            builder.HasOne(h => h.Product)
                .WithMany()
                .HasForeignKey(h => h.IdProduct)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(h => h.Center)
                .WithMany()
                .HasForeignKey(h => h.IdCenter)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
