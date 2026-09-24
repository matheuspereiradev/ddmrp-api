using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Service.Domain.Entities;

namespace Service.Infra.Data.EntitiesConfiguration
{
    public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
    {
        public void Configure(EntityTypeBuilder<RolePermission> builder)
        {
            builder.ToTable("RolePermissions");
            builder.HasKey(rp => new { rp.IdRole, rp.IdPermission });

            builder.HasOne(rp => rp.Role)
                .WithMany()
                .HasForeignKey(rp => rp.IdRole)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(rp => rp.Permission)
                .WithMany()
                .HasForeignKey(rp => rp.IdPermission)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
