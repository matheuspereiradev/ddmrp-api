using NSubstitute;
using Service.Application.DTOs.Holiday;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;

namespace Service.Tests.Application;

public class HolidayServiceTests
{
    private readonly IHolidayRepository _holidayRepository = Substitute.For<IHolidayRepository>();
    private readonly HolidayService _sut;

    public HolidayServiceTests()
    {
        _sut = new HolidayService(_holidayRepository);
    }

    [Fact]
    public async Task AddAsync_CreatesSingleHoliday_WhenNotRecurring()
    {
        var postDto = new HolidayPostDto { Name = "Company Day", Date = new DateTime(2026, 12, 25), IsRecurring = false };
        _holidayRepository.AddAsync(Arg.Any<Holiday>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Holiday>());

        var result = await _sut.AddAsync(postDto);

        var holiday = Assert.Single(result);
        Assert.Equal("Company Day", holiday.Name);
        Assert.Equal(new DateTime(2026, 12, 25), holiday.Date);
        await _holidayRepository.Received(1).AddAsync(Arg.Any<Holiday>(), Arg.Any<CancellationToken>());
        await _holidayRepository.DidNotReceive().AddRangeAsync(Arg.Any<List<Holiday>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_Creates15Years_WhenRecurring()
    {
        var postDto = new HolidayPostDto { Name = "Christmas", Date = new DateTime(2026, 12, 25), IsRecurring = true };
        _holidayRepository.AddRangeAsync(Arg.Any<List<Holiday>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<List<Holiday>>());

        var result = await _sut.AddAsync(postDto);

        Assert.Equal(15, result.Count);
        Assert.All(result, h => Assert.Equal("Christmas", h.Name));
        Assert.Equal(new DateTime(2026, 12, 25), result[0].Date);
        Assert.Equal(new DateTime(2040, 12, 25), result[14].Date);
    }

    [Fact]
    public async Task GetAllAsync_DefaultsIncludePastToFalse()
    {
        _holidayRepository.GetFilteredAsync(1, 10, false, Arg.Any<CancellationToken>())
            .Returns(new PagedList<Holiday>([], 1, 10, 0));

        await _sut.GetAllAsync(1, 10);

        await _holidayRepository.Received(1).GetFilteredAsync(1, 10, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllAsync_PassesIncludePastThrough_WhenTrue()
    {
        var holiday = new Holiday { Id = 1, Name = "Old Holiday", Date = new DateTime(2020, 1, 1) };
        _holidayRepository.GetFilteredAsync(1, 10, true, Arg.Any<CancellationToken>())
            .Returns(new PagedList<Holiday>([holiday], 1, 10, 1));

        var result = await _sut.GetAllAsync(1, 10, includePast: true);

        Assert.Equal("Old Holiday", Assert.Single(result).Name);
        await _holidayRepository.Received(1).GetFilteredAsync(1, 10, true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ThrowsNotFoundException_WhenHolidayDoesNotExist()
    {
        _holidayRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Holiday)null!);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UpdateAsync(1, new HolidayPutDto { Name = "X", Date = DateTime.Today }));
    }

    [Fact]
    public async Task DeleteAsync_ThrowsNotFoundException_WhenHolidayDoesNotExist()
    {
        _holidayRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns((Holiday)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(1));
    }
}
