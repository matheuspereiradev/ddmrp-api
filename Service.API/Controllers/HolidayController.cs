using Service.API.Extensions;
using Service.API.Filters;
using Service.API.Models;
using Service.Application.DTOs.Holiday;
using Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HolidayController : Controller
    {
        private readonly IHolidayService _holidayService;

        public HolidayController(IHolidayService holidayService)
        {
            _holidayService = holidayService;
        }

        [HttpPost]
        [Authorize]
        [RequirePermission]
        public async Task<ActionResult> CreateHoliday(HolidayPostDto holidayPostDto, CancellationToken cancellationToken)
        {
            var holidays = await _holidayService.AddAsync(holidayPostDto, cancellationToken);
            return Ok(holidays);
        }

        [HttpGet]
        [Authorize]
        [RequirePermission]
        public async Task<ActionResult> GetAllHolidays([FromQuery] bool includePast, [FromQuery] PaginationParams paginationParams, CancellationToken cancellationToken)
        {
            var holidays = await _holidayService.GetAllAsync(paginationParams.PageNumber, paginationParams.PageSize, includePast, cancellationToken);

            Response.AddPaginationHeader(
                new PaginationHeader(paginationParams.PageNumber, paginationParams.PageSize, holidays.TotalCount, holidays.TotalPages));

            return Ok(holidays);
        }

        [HttpPut("{id}")]
        [Authorize]
        [RequirePermission]
        public async Task<ActionResult> UpdateHoliday(int id, HolidayPutDto holidayPutDto, CancellationToken cancellationToken)
        {
            var holiday = await _holidayService.UpdateAsync(id, holidayPutDto, cancellationToken);
            return Ok(holiday);
        }

        [HttpDelete("{id}")]
        [Authorize]
        [RequirePermission]
        public async Task<ActionResult> DeleteHoliday(int id, CancellationToken cancellationToken)
        {
            var holiday = await _holidayService.DeleteAsync(id, cancellationToken);
            return Ok(holiday);
        }
    }
}
