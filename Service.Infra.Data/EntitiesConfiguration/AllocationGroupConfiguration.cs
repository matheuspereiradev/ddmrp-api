using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Service.Domain.Entities;

namespace Service.Infra.Data.EntitiesConfiguration
{
    public class AllocationGroupConfiguration : IEntityTypeConfiguration<AllocationGroup>
    {
        public void Configure(EntityTypeBuilder<AllocationGroup> builder)
        {
            builder.ToTable("AllocationGroups");
            builder.ConfigureAuditFields();
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Name).IsRequired().HasMaxLength(100);
        }
    }
}
