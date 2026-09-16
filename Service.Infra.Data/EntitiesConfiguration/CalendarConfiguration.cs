using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Service.Domain.Entities;

namespace Service.Infra.Data.EntitiesConfiguration
{
    public class CalendarConfiguration : IEntityTypeConfiguration<Calendar>
    {
        public void Configure(EntityTypeBuilder<Calendar> builder)
        {
            builder.ToTable("Calendar");
            builder.HasKey(c => c.Date);
            builder.Property(c => c.Date).IsRequired();
            builder.Property(c => c.DayOfWeekNumber).IsRequired();
            builder.Property(c => c.IsWorkingDay).IsRequired().HasDefaultValue(true);
        }
    }
}
