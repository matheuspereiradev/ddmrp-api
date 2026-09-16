using System;

namespace Service.Domain.Entities
{
    public class Calendar
    {
        public DateTime Date { get; set; }

        // (int)Date.DayOfWeek, stored so business-day filtering doesn't depend on translating
        // DateTime.DayOfWeek in a query (unsupported by the SqlServer provider here).
        public int DayOfWeekNumber { get; set; }

        // Whether this date counts as a business day for the Forecast daily breakdown and the
        // Adu "Futuro" calculation. Replaces the old Settings-driven weekday lookup (Setting is
        // no longer read by either of those) — set directly per date instead.
        public bool IsWorkingDay { get; set; }
    }
}
