using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Service.Domain.Entities;

namespace Service.Infra.Data.EntitiesConfiguration
{
    public class ReasonGroupConfiguration : IEntityTypeConfiguration<ReasonGroup>
    {
        public void Configure(EntityTypeBuilder<ReasonGroup> builder)
        {
            builder.ToTable("ReasonGroups");
            builder.ConfigureAuditFields();
            builder.HasKey(rg => rg.Id);
            builder.Property(rg => rg.Name).IsRequired().HasMaxLength(100);
            builder.Property(rg => rg.IsFromSystem).IsRequired().HasDefaultValue(false);
        }
    }
}
