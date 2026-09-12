using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Service.Domain.Entities;

namespace Service.Infra.Data.EntitiesConfiguration
{
    public class MasterBufferConfiguration : IEntityTypeConfiguration<MasterBuffer>
    {
        public void Configure(EntityTypeBuilder<MasterBuffer> builder)
        {
            builder.ToTable("MasterBuffers");
            builder.ConfigureAuditFields();
            builder.HasKey(mb => mb.Id);

            builder.Property(mb => mb.IdProduct).IsRequired();
            builder.Property(mb => mb.IdCenter).IsRequired();
            builder.Property(mb => mb.IdProductFather).IsRequired();
            builder.Property(mb => mb.IdCenterFather).IsRequired();
            builder.Property(mb => mb.Sequency).IsRequired();

            builder.HasOne(mb => mb.Product)
                .WithMany()
                .HasForeignKey(mb => mb.IdProduct)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(mb => mb.Center)
                .WithMany()
                .HasForeignKey(mb => mb.IdCenter)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(mb => mb.ProductFather)
                .WithMany()
                .HasForeignKey(mb => mb.IdProductFather)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(mb => mb.CenterFather)
                .WithMany()
                .HasForeignKey(mb => mb.IdCenterFather)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
