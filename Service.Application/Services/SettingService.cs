using Service.Application.DTOs.Setting;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Domain.Interfaces;

namespace Service.Application.Services
{
    public class SettingService : ISettingService
    {
        private const int SingletonId = 1;

        private readonly ISettingRepository _settingRepository;
        private readonly ICalendarRepository _calendarRepository;

        public SettingService(ISettingRepository settingRepository, ICalendarRepository calendarRepository)
        {
            _settingRepository = settingRepository;
            _calendarRepository = calendarRepository;
        }

        public async Task<SettingGetDto> GetAsync(CancellationToken cancellationToken = default)
        {
            var setting = await _settingRepository.GetByIdAsync(SingletonId, cancellationToken);
            if (setting == null)
                throw new NotFoundException("Setting not found.");

            return ToGetDto(setting);
        }

        public async Task<SettingGetDto> UpdateWorkingDaysAsync(SettingPutDto putDto, CancellationToken cancellationToken = default)
        {
            var setting = await _settingRepository.GetByIdAsync(SingletonId, cancellationToken);
            if (setting == null)
                throw new NotFoundException("Setting not found.");

            var today = DateTime.UtcNow.Date;

            // Always reapplies every weekday (not just the ones that changed since the last PUT) —
            // Calendar is a separate table that can drift out of sync with Setting (e.g. a prior bug,
            // or a manual edit), so this must be able to self-heal instead of trusting Setting's own
            // previous value as a proxy for Calendar's actual current state.
            var weekdays = new[]
            {
                (DayOfWeek.Monday, putDto.MondayIsWorkingDay),
                (DayOfWeek.Tuesday, putDto.TuesdayIsWorkingDay),
                (DayOfWeek.Wednesday, putDto.WednesdayIsWorkingDay),
                (DayOfWeek.Thursday, putDto.ThursdayIsWorkingDay),
                (DayOfWeek.Friday, putDto.FridayIsWorkingDay),
                (DayOfWeek.Saturday, putDto.SaturdayIsWorkingDay),
                (DayOfWeek.Sunday, putDto.SundayIsWorkingDay)
            };

            foreach (var (dayOfWeek, isWorkingDay) in weekdays)
                await _calendarRepository.UpdateFutureWorkingDaysAsync(dayOfWeek, isWorkingDay, today, cancellationToken);

            setting.MondayIsWorkingDay = putDto.MondayIsWorkingDay;
            setting.TuesdayIsWorkingDay = putDto.TuesdayIsWorkingDay;
            setting.WednesdayIsWorkingDay = putDto.WednesdayIsWorkingDay;
            setting.ThursdayIsWorkingDay = putDto.ThursdayIsWorkingDay;
            setting.FridayIsWorkingDay = putDto.FridayIsWorkingDay;
            setting.SaturdayIsWorkingDay = putDto.SaturdayIsWorkingDay;
            setting.SundayIsWorkingDay = putDto.SundayIsWorkingDay;

            var updated = await _settingRepository.UpdateAsync(setting, cancellationToken);
            return ToGetDto(updated);
        }

        private static SettingGetDto ToGetDto(Domain.Entities.Setting entity) => new()
        {
            Id = entity.Id,
            MondayIsWorkingDay = entity.MondayIsWorkingDay,
            TuesdayIsWorkingDay = entity.TuesdayIsWorkingDay,
            WednesdayIsWorkingDay = entity.WednesdayIsWorkingDay,
            ThursdayIsWorkingDay = entity.ThursdayIsWorkingDay,
            FridayIsWorkingDay = entity.FridayIsWorkingDay,
            SaturdayIsWorkingDay = entity.SaturdayIsWorkingDay,
            SundayIsWorkingDay = entity.SundayIsWorkingDay
        };
    }
}
