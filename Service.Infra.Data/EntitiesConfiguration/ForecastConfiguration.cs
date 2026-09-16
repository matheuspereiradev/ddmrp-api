using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Service.Domain.Entities;

namespace Service.Infra.Data.EntitiesConfiguration
{
    public class ForecastConfiguration : IEntityTypeConfiguration<Forecast>
    {
        public void Configure(EntityTypeBuilder<Forecast> builder)
        {
            builder.ToTable("Forecasts");
            builder.ConfigureAuditFields();
            builder.HasKey(f => f.Id);

            builder.Property(f => f.IdProduct).IsRequired();
            builder.Property(f => f.IdCenter).IsRequired();
            builder.Property(f => f.Value).IsRequired().HasPrecision(18, 4);
            builder.Property(f => f.StartDate).IsRequired();
            builder.Property(f => f.EndDate).IsRequired();

            builder.HasOne(f => f.Product)
                .WithMany()
                .HasForeignKey(f => f.IdProduct)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(f => f.Center)
                .WithMany()
                .HasForeignKey(f => f.IdCenter)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
