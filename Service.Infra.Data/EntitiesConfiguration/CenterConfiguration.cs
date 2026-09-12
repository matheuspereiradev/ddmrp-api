using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Service.Domain.Entities;

namespace Service.Infra.Data.EntitiesConfiguration
{
    public class CenterConfiguration : IEntityTypeConfiguration<Center>
    {
        public void Configure(EntityTypeBuilder<Center> builder)
        {
            builder.ToTable("Centers");
            builder.ConfigureAuditFields();
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Code).IsRequired().HasMaxLength(20);
            builder.Property(c => c.Description).IsRequired().HasMaxLength(200);
            builder.Property(c => c.City).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Zone).IsRequired().HasMaxLength(100);
        }
    }
}
