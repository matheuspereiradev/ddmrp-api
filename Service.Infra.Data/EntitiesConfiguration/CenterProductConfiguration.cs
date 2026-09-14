using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Service.Domain.Entities;
using Service.Domain.Enums;

namespace Service.Infra.Data.EntitiesConfiguration
{
    public class CenterProductConfiguration : IEntityTypeConfiguration<CenterProduct>
    {
        public void Configure(EntityTypeBuilder<CenterProduct> builder)
        {
            builder.ToTable("CenterProducts");
            builder.ConfigureAuditFields();
            builder.HasKey(cp => cp.Id);

            builder.Property(cp => cp.IdProduct).IsRequired();
            builder.Property(cp => cp.IdCenter).IsRequired();
            builder.Property(cp => cp.PackQuantity).IsRequired().HasPrecision(18, 4);
            builder.Property(cp => cp.Moq).IsRequired().HasPrecision(18, 4);
            builder.Property(cp => cp.LeadTime).IsRequired();
            builder.Property(cp => cp.Frequency).IsRequired();
            builder.Property(cp => cp.Class).HasMaxLength(200);
            builder.Property(cp => cp.Classification).HasMaxLength(200);
            builder.Property(cp => cp.Segment).HasMaxLength(200);
            builder.Property(cp => cp.Stock).IsRequired().HasPrecision(18, 4);
            builder.Property(cp => cp.Adu).HasPrecision(18, 4);
            builder.Property(cp => cp.Adi).HasPrecision(18, 4);
            builder.Property(cp => cp.StandardDeviation).HasPrecision(18, 4);
            builder.Property(cp => cp.Cv).HasPrecision(18, 4);
            builder.Property(cp => cp.UseSuggestedLTFactor).IsRequired().HasDefaultValue(true);
            builder.Property(cp => cp.UseSuggestedVariabilityFactor).IsRequired().HasDefaultValue(true);
            builder.Property(cp => cp.RedZoneBase).HasPrecision(18, 4);
            builder.Property(cp => cp.RedZoneSafe).HasPrecision(18, 4);
            builder.Ignore(cp => cp.RedZone);
            builder.Property(cp => cp.YellowZone).HasPrecision(18, 4);
            builder.Property(cp => cp.GreenZone).HasPrecision(18, 4);
            builder.Property(cp => cp.UseDafOnGreenZone).IsRequired().HasDefaultValue(false);
            builder.Property(cp => cp.CustomLeadTimeFactor).IsRequired().HasPrecision(18, 4).HasDefaultValue(1);
            builder.Property(cp => cp.CustomVariabilityFactor).IsRequired().HasPrecision(18, 4).HasDefaultValue(1);
            builder.Property(cp => cp.GreenZoneParametrizationUseMoq).IsRequired().HasDefaultValue(true);
            builder.Property(cp => cp.GreenZoneParametrizationUseAduXFrequency).IsRequired().HasDefaultValue(true);
            builder.Property(cp => cp.GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime).IsRequired().HasDefaultValue(true);
            builder.Property(cp => cp.BufferType).IsRequired().HasDefaultValue(BufferType.Normal);
            builder.Property(cp => cp.ZafRedZone).IsRequired().HasPrecision(18, 4).HasDefaultValue(0);
            builder.Property(cp => cp.ZafYellowZone).IsRequired().HasPrecision(18, 4).HasDefaultValue(0);
            builder.Property(cp => cp.ZafGreenZone).IsRequired().HasPrecision(18, 4).HasDefaultValue(0);
            builder.Property(cp => cp.QualifiedDemand).HasPrecision(18, 4);
            builder.Property(cp => cp.SpikeHorizonType).IsRequired();
            builder.Property(cp => cp.SpikeHorizonValue).IsRequired().HasDefaultValue(60);
            builder.Property(cp => cp.SpikeHorizonLTDays).IsRequired().HasDefaultValue(1);
            builder.Property(cp => cp.SpikeThresholdType).IsRequired();
            builder.Property(cp => cp.SpikeThresholdAdu).IsRequired().HasPrecision(18, 4).HasDefaultValue(1);
            builder.Property(cp => cp.SpikeThresholdPercentageRedZone).IsRequired().HasPrecision(18, 4).HasDefaultValue(0.5);

            builder.HasOne(cp => cp.Product)
                .WithMany()
                .HasForeignKey(cp => cp.IdProduct)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(cp => cp.Center)
                .WithMany()
                .HasForeignKey(cp => cp.IdCenter)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(cp => cp.OriginCenter)
                .WithMany()
                .HasForeignKey(cp => cp.IdOriginCenter)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(cp => cp.Provider)
                .WithMany()
                .HasForeignKey(cp => cp.IdProvider)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(cp => cp.Tag)
                .WithMany()
                .HasForeignKey(cp => cp.IdTag)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(cp => cp.Reason)
                .WithMany()
                .HasForeignKey(cp => cp.IdReason)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(cp => cp.AllocationGroup)
                .WithMany()
                .HasForeignKey(cp => cp.IdAllocationGroup)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(cp => cp.BufferProfile)
                .WithMany()
                .HasForeignKey(cp => cp.IdBufferProfile)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
