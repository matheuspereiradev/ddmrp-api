namespace Service.Domain.Interfaces
{
    public interface ICalendarRepository
    {
        Task UpdateFutureWorkingDaysAsync(DayOfWeek dayOfWeek, bool isWorkingDay, DateTime afterDate, CancellationToken cancellationToken = default);
    }
}
