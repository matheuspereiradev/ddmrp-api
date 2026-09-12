using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Service.Domain.Entities;

namespace Service.Infra.Data.EntitiesConfiguration
{
    public class NoteConfiguration : IEntityTypeConfiguration<Note>
    {
        public void Configure(EntityTypeBuilder<Note> builder)
        {
            builder.ToTable("Notes");
            builder.ConfigureAuditFields();
            builder.HasKey(n => n.Id);

            builder.Property(n => n.Content).IsRequired().HasMaxLength(1000);
            builder.Property(n => n.CenterProductId).IsRequired();

            builder.HasOne(n => n.CenterProduct)
                .WithMany()
                .HasForeignKey(n => n.CenterProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(n => n.CreatedByUser)
                .WithMany()
                .HasForeignKey(n => n.createdBy)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
