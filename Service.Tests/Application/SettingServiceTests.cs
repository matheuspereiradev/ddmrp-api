using NSubstitute;
using Service.Application.DTOs.Setting;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Tests.Application;

public class SettingServiceTests
{
    private readonly ISettingRepository _settingRepository = Substitute.For<ISettingRepository>();
    private readonly ICalendarRepository _calendarRepository = Substitute.For<ICalendarRepository>();
    private readonly SettingService _sut;

    public SettingServiceTests()
    {
        _sut = new SettingService(_settingRepository, _calendarRepository);
    }

    private static Setting BuildSetting() => new()
    {
        Id = 1,
        MondayIsWorkingDay = true,
        TuesdayIsWorkingDay = true,
        WednesdayIsWorkingDay = true,
        ThursdayIsWorkingDay = true,
        FridayIsWorkingDay = true,
        SaturdayIsWorkingDay = false,
        SundayIsWorkingDay = false
    };

    private static SettingPutDto BuildPutDto(bool friday) => new()
    {
        MondayIsWorkingDay = true,
        TuesdayIsWorkingDay = true,
        WednesdayIsWorkingDay = true,
        ThursdayIsWorkingDay = true,
        FridayIsWorkingDay = friday,
        SaturdayIsWorkingDay = false,
        SundayIsWorkingDay = false
    };

    [Fact]
    public async Task GetAsync_ThrowsNotFoundException_WhenSettingDoesNotExist()
    {
        _settingRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Setting)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetAsync());
    }

    [Fact]
    public async Task UpdateWorkingDaysAsync_PropagatesEveryWeekday_ToCalendar_EvenUnchangedOnes()
    {
        var setting = BuildSetting();
        _settingRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(setting);
        _settingRepository.UpdateAsync(Arg.Any<Setting>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Setting>());

        var result = await _sut.UpdateWorkingDaysAsync(BuildPutDto(friday: false));

        Assert.False(result.FridayIsWorkingDay);
        await _calendarRepository.Received(1).UpdateFutureWorkingDaysAsync(
            DayOfWeek.Friday, false, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        // Monday didn't change (true -> true) but must still be reapplied, so Calendar can
        // self-heal even if it had drifted out of sync with Setting for some other reason.
        await _calendarRepository.Received(1).UpdateFutureWorkingDaysAsync(
            DayOfWeek.Monday, true, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateWorkingDaysAsync_OnlyMonday_StillReappliesAllSevenWeekdays()
    {
        var setting = BuildSetting(); // Mon-Fri true, Sat/Sun false (default)
        _settingRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(setting);
        _settingRepository.UpdateAsync(Arg.Any<Setting>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Setting>());

        var putDto = new SettingPutDto
        {
            MondayIsWorkingDay = true,
            TuesdayIsWorkingDay = false,
            WednesdayIsWorkingDay = false,
            ThursdayIsWorkingDay = false,
            FridayIsWorkingDay = false,
            SaturdayIsWorkingDay = false,
            SundayIsWorkingDay = false
        };

        await _sut.UpdateWorkingDaysAsync(putDto);

        await _calendarRepository.Received(1).UpdateFutureWorkingDaysAsync(DayOfWeek.Monday, true, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _calendarRepository.Received(1).UpdateFutureWorkingDaysAsync(DayOfWeek.Tuesday, false, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _calendarRepository.Received(1).UpdateFutureWorkingDaysAsync(DayOfWeek.Wednesday, false, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _calendarRepository.Received(1).UpdateFutureWorkingDaysAsync(DayOfWeek.Thursday, false, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _calendarRepository.Received(1).UpdateFutureWorkingDaysAsync(DayOfWeek.Friday, false, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _calendarRepository.Received(1).UpdateFutureWorkingDaysAsync(DayOfWeek.Saturday, false, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _calendarRepository.Received(1).UpdateFutureWorkingDaysAsync(DayOfWeek.Sunday, false, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateWorkingDaysAsync_ThrowsNotFoundException_WhenSettingDoesNotExist()
    {
        _settingRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Setting)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateWorkingDaysAsync(BuildPutDto(friday: false)));
    }
}
