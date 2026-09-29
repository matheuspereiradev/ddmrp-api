using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Service.Domain.Entities;

namespace Service.Infra.Data.EntitiesConfiguration
{
    public class ImporterConfiguration : IEntityTypeConfiguration<Importer>
    {
        public void Configure(EntityTypeBuilder<Importer> builder)
        {
            builder.ToTable("Importers");
            builder.ConfigureAuditFields();
            builder.HasKey(i => i.Id);
            builder.Property(i => i.Name).IsRequired().HasMaxLength(100);
            builder.Property(i => i.Description).HasMaxLength(500);
            builder.Property(i => i.ProcedureName).IsRequired().HasMaxLength(128);
        }
    }
}
