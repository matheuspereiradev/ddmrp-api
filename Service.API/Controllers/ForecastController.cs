using Service.API.Extensions;
using Service.API.Models;
using Service.Application.DTOs.Forecast;
using Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ForecastController : Controller
    {
        private readonly IForecastService _forecastService;

        public ForecastController(IForecastService forecastService)
        {
            _forecastService = forecastService;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult> CreateForecast(ForecastPostDto forecastPostDto, CancellationToken cancellationToken)
        {
            var forecast = await _forecastService.AddAsync(forecastPostDto, cancellationToken);
            return Ok(forecast);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> GetAllForecasts(
            [FromQuery] int? idProduct,
            [FromQuery] int? idCenter,
            [FromQuery] DateTime? dateStart,
            [FromQuery] DateTime? dateEnd,
            [FromQuery] PaginationParams paginationParams,
            CancellationToken cancellationToken)
        {
            var forecasts = await _forecastService.GetFilteredAsync(idProduct, idCenter, dateStart, dateEnd, paginationParams.PageNumber, paginationParams.PageSize, cancellationToken);

            Response.AddPaginationHeader(
                new PaginationHeader(paginationParams.PageNumber, paginationParams.PageSize, forecasts.TotalCount, forecasts.TotalPages));

            return Ok(forecasts);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult> UpdateForecast(int id, ForecastPutDto forecastPutDto, CancellationToken cancellationToken)
        {
            var forecast = await _forecastService.UpdateAsync(id, forecastPutDto, cancellationToken);
            return Ok(forecast);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteForecast(int id, CancellationToken cancellationToken)
        {
            var forecast = await _forecastService.DeleteAsync(id, cancellationToken);
            return Ok(forecast);
        }
    }
}
