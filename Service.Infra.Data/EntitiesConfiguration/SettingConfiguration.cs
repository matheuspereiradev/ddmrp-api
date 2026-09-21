using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Service.Domain.Entities;

namespace Service.Infra.Data.EntitiesConfiguration
{
    public class SettingConfiguration : IEntityTypeConfiguration<Setting>
    {
        public void Configure(EntityTypeBuilder<Setting> builder)
        {
            builder.ToTable("Settings");
            builder.ConfigureAuditFields();
            builder.HasKey(s => s.Id);

            builder.Property(s => s.MondayIsWorkingDay).IsRequired().HasDefaultValue(true);
            builder.Property(s => s.TuesdayIsWorkingDay).IsRequired().HasDefaultValue(true);
            builder.Property(s => s.WednesdayIsWorkingDay).IsRequired().HasDefaultValue(true);
            builder.Property(s => s.ThursdayIsWorkingDay).IsRequired().HasDefaultValue(true);
            builder.Property(s => s.FridayIsWorkingDay).IsRequired().HasDefaultValue(true);
            builder.Property(s => s.SaturdayIsWorkingDay).IsRequired().HasDefaultValue(false);
            builder.Property(s => s.SundayIsWorkingDay).IsRequired().HasDefaultValue(false);
            builder.Property(s => s.ClientName).HasMaxLength(100);
            builder.Property(s => s.WorkspaceId).HasMaxLength(100);

            // Single global config row — seeded here (not via a CRUD endpoint, none exists yet)
            // so the Forecast daily-explosion logic always has a row to read.
            builder.HasData(new Setting
            {
                Id = 1,
                MondayIsWorkingDay = true,
                TuesdayIsWorkingDay = true,
                WednesdayIsWorkingDay = true,
                ThursdayIsWorkingDay = true,
                FridayIsWorkingDay = true,
                SaturdayIsWorkingDay = false,
                SundayIsWorkingDay = false
            });
        }
    }
}
