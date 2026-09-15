using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Service.Domain.Entities;

namespace Service.Infra.Data.EntitiesConfiguration
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders");
            builder.ConfigureAuditFields();
            builder.HasKey(o => o.Id);

            builder.Property(o => o.OrderNumber).IsRequired().HasMaxLength(50);
            builder.Property(o => o.IdProduct).IsRequired();
            builder.Property(o => o.Quantity).IsRequired().HasPrecision(18, 4);
            builder.Property(o => o.DeliveredQuantity).IsRequired().HasPrecision(18, 4);
            builder.Property(o => o.MeasurementUnit).IsRequired().HasMaxLength(20);
            builder.Property(o => o.CreationDate).IsRequired();
            builder.Property(o => o.Notes).HasMaxLength(500);
            builder.Property(o => o.Type).IsRequired();
            builder.Property(o => o.IsInbound).IsRequired();
            builder.Property(o => o.IsOutbound).IsRequired();
            builder.Property(o => o.IsFictional).IsRequired().HasDefaultValue(false);

            builder.Ignore(o => o.PendingQuantity);
            builder.Ignore(o => o.OrderLeadtime);

            builder.HasOne(o => o.Product)
                .WithMany()
                .HasForeignKey(o => o.IdProduct)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(o => o.Partner)
                .WithMany()
                .HasForeignKey(o => o.IdPartner)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(o => o.DestinyCenter)
                .WithMany()
                .HasForeignKey(o => o.IdDestinyCenter)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(o => o.OriginCenter)
                .WithMany()
                .HasForeignKey(o => o.IdOriginCenter)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
