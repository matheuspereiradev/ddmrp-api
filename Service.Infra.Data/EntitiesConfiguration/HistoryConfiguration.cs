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
