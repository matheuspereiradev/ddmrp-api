using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Service.Domain.Entities;

namespace Service.Infra.Data.EntitiesConfiguration
{
    public class ExporterConfiguration : IEntityTypeConfiguration<Exporter>
    {
        public void Configure(EntityTypeBuilder<Exporter> builder)
        {
            builder.ToTable("Exporters");
            builder.ConfigureAuditFields();
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
            builder.Property(e => e.Description).HasMaxLength(500);
            builder.Property(e => e.ProcedureName).IsRequired().HasMaxLength(128);
        }
    }
}
