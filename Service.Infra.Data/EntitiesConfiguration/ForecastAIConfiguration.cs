using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Service.Domain.Entities;
using Service.Domain.Enums;

namespace Service.Infra.Data.EntitiesConfiguration
{
    public class ForecastAIConfiguration : IEntityTypeConfiguration<ForecastAI>
    {
        public void Configure(EntityTypeBuilder<ForecastAI> builder)
        {
            builder.ToTable("ForecastAIs");
            builder.ConfigureAuditFields();
            builder.HasKey(f => f.Id);

            builder.Property(f => f.IdCenter).IsRequired();
            builder.Property(f => f.IdProduct).IsRequired();
            builder.Property(f => f.MonthYear).IsRequired();
            builder.Property(f => f.PreviewType).IsRequired();
            builder.Property(f => f.Assertiveness).IsRequired().HasPrecision(18, 4);
            builder.Property(f => f.QuantityPreviewed).IsRequired().HasPrecision(18, 4);
            builder.Property(f => f.PreviewState).IsRequired().HasDefaultValue(ForecastPreviewState.NotReviewed);

            builder.HasOne(f => f.Center)
                .WithMany()
                .HasForeignKey(f => f.IdCenter)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(f => f.Product)
                .WithMany()
                .HasForeignKey(f => f.IdProduct)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
