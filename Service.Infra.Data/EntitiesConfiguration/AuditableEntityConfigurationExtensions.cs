using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Service.Domain.Entities;

namespace Service.Infra.Data.EntitiesConfiguration
{
    public static class AuditableEntityConfigurationExtensions
    {
        public static void ConfigureAuditFields<T>(this EntityTypeBuilder<T> builder) where T : BaseEntity
        {
            builder.Property(e => e.createdAt).HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
