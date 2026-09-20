using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Service.Domain.Entities;

namespace Service.Infra.Data.EntitiesConfiguration
{
    public class BufferProfileConfiguration : IEntityTypeConfiguration<BufferProfile>
    {
        public void Configure(EntityTypeBuilder<BufferProfile> builder)
        {
            builder.ToTable("BufferProfiles");
            builder.ConfigureAuditFields();
            builder.HasKey(b => b.Id);

            builder.Property(b => b.ProfileName).IsRequired().HasMaxLength(25);
            builder.Property(b => b.SupplyType).IsRequired();
            builder.Property(b => b.LeadTimeCategory).IsRequired();
            builder.Property(b => b.VariabilityCategory).IsRequired();
            builder.Property(b => b.LeadTimeFactor).IsRequired().HasPrecision(18, 4);
            builder.Property(b => b.VariabilityFactor).IsRequired().HasPrecision(18, 4);
            builder.Property(b => b.AduCalculationDays).IsRequired();
            builder.Property(b => b.AduFutureDays).IsRequired();
            builder.Property(b => b.Frequency).IsRequired();
            builder.Property(b => b.GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime).IsRequired();
            builder.Property(b => b.GreenZoneParametrizationUseMoq).IsRequired();
            builder.Property(b => b.GreenZoneParametrizationUseAduXFrequency).IsRequired();
            builder.Property(b => b.SpikeHorizonType).IsRequired();
            builder.Property(b => b.SpikeHorizonValue).IsRequired();
            builder.Property(b => b.SpikeHorizonLTDays).IsRequired();
            builder.Property(b => b.SpikeThresholdType).IsRequired();
            builder.Property(b => b.SpikeThresholdAdu).IsRequired();
            builder.Property(b => b.SpikeThresholdPercentageRedZone).IsRequired().HasPrecision(18, 4);
            builder.Property(b => b.IsActive).IsRequired();
            builder.Property(b => b.IsMakeToOrder).IsRequired();
        }
    }
}
