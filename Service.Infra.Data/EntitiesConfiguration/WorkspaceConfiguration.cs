using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Service.Domain.Entities;

namespace Service.Infra.Data.EntitiesConfiguration
{
    public class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
    {
        public void Configure(EntityTypeBuilder<Workspace> builder)
        {
            builder.ToTable("Workspaces");
            builder.ConfigureAuditFields();
            builder.HasKey(w => w.Id);

            builder.Property(w => w.IdCenter).IsRequired();
            builder.Property(w => w.IdProduct).IsRequired();
            builder.Property(w => w.IdUser).IsRequired();
            builder.Property(w => w.OptimizedQuantity).IsRequired().HasPrecision(18, 4);
            builder.Property(w => w.Approved).IsRequired();

            builder.HasOne(w => w.Center)
                .WithMany()
                .HasForeignKey(w => w.IdCenter)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(w => w.Product)
                .WithMany()
                .HasForeignKey(w => w.IdProduct)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.IdUser)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
