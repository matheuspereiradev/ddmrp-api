using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Service.Domain.Entities;

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
