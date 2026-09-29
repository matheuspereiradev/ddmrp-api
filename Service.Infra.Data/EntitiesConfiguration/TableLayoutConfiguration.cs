using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Service.Domain.Entities;

namespace Service.Infra.Data.EntitiesConfiguration
{
    public class TableLayoutConfiguration : IEntityTypeConfiguration<TableLayout>
    {
        public void Configure(EntityTypeBuilder<TableLayout> builder)
        {
            builder.ToTable("TableLayouts");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.TableName).IsRequired().HasMaxLength(100);
            builder.Property(t => t.CreatedAt).IsRequired();
            builder.Property(t => t.UpdatedAt).IsRequired();
            builder.HasIndex(t => new { t.UserId, t.TableName }).IsUnique();

            builder.OwnsMany(t => t.Columns, columns =>
            {
                columns.ToJson("Columns");
                columns.Property(c => c.ColumnId).IsRequired().HasMaxLength(100);
                columns.Property(c => c.Label).HasMaxLength(100);
                columns.Property(c => c.Color).HasMaxLength(7);
            });

            builder.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
