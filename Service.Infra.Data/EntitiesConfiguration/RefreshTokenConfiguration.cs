using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Service.Domain.Entities;

namespace Service.Infra.Data.EntitiesConfiguration
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens");
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Token).IsRequired().HasMaxLength(500);
            builder.HasIndex(r => r.Token).IsUnique();
            builder.Property(r => r.CreatedAt).IsRequired();
            builder.Property(r => r.ExpiresAt).IsRequired();
            builder.Property(r => r.ReplacedByToken).HasMaxLength(500);

            builder.Ignore(r => r.IsExpired);
            builder.Ignore(r => r.IsRevoked);
            builder.Ignore(r => r.IsActive);

            builder.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
