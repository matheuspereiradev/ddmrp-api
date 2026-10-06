using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Service.Domain.Entities;

namespace Service.Infra.Data.EntitiesConfiguration
{
    public class UserCenterConfiguration : IEntityTypeConfiguration<UserCenter>
    {
        public void Configure(EntityTypeBuilder<UserCenter> builder)
        {
            builder.ToTable("UserCenters");
            builder.HasKey(uc => uc.Id);

            builder.Property(uc => uc.IdUser).IsRequired();
            builder.Property(uc => uc.IdCenter).IsRequired();
            builder.HasIndex(uc => new { uc.IdUser, uc.IdCenter }).IsUnique();

            builder.HasOne(uc => uc.User)
                .WithMany()
                .HasForeignKey(uc => uc.IdUser)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(uc => uc.Center)
                .WithMany()
                .HasForeignKey(uc => uc.IdCenter)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
