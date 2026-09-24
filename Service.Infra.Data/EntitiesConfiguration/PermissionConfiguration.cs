using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Service.Domain.Entities;

namespace Service.Infra.Data.EntitiesConfiguration
{
    public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            builder.ToTable("Permissions");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).HasMaxLength(300);
            builder.Property(p => p.Description).HasMaxLength(200);
            builder.Property(p => p.Module).HasMaxLength(100);

            builder.HasData(
                new Permission { Id = "center:POST", Description = "Create center", Module = "Center" },
                new Permission { Id = "center:GET", Description = "List centers", Module = "Center" },
                new Permission { Id = "center:PUT", Description = "Update center", Module = "Center" },
                new Permission { Id = "center:DELETE", Description = "Delete center", Module = "Center" }
            );
        }
    }
}
