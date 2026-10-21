using Service.API.Extensions;
using Service.API.Models;
using Service.Application.DTOs.ForecastAI;
using Service.Application.Interfaces;
using Service.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ForecastAIController : Controller
    {
        private readonly IForecastAIService _forecastAIService;

        public ForecastAIController(IForecastAIService forecastAIService)
        {
            _forecastAIService = forecastAIService;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> GetAllForecastAIs(
            [FromQuery] int? idCenter,
            [FromQuery] int? idProduct,
            [FromQuery] DateTime? monthYear,
            [FromQuery] ForecastPreviewType? previewType,
            [FromQuery] ForecastPreviewState? previewState,
            [FromQuery] PaginationParams paginationParams,
            CancellationToken cancellationToken)
        {
            var forecastAIs = await _forecastAIService.GetFilteredAsync(idCenter, idProduct, monthYear, previewType, previewState, paginationParams.PageNumber, paginationParams.PageSize, cancellationToken);

            Response.AddPaginationHeader(
                new PaginationHeader(paginationParams.PageNumber, paginationParams.PageSize, forecastAIs.TotalCount, forecastAIs.TotalPages));

            return Ok(forecastAIs);
        }

        [HttpPost("apply")]
        [Authorize]
        public async Task<ActionResult> Apply(ApplyForecastAIDto applyForecastAIDto, CancellationToken cancellationToken)
        {
            var result = await _forecastAIService.ApplyAsync(applyForecastAIDto.Id, cancellationToken);
            return Ok(result);
        }

        [HttpPost("reject")]
        [Authorize]
        public async Task<ActionResult> Reject(RejectForecastAIDto rejectForecastAIDto, CancellationToken cancellationToken)
        {
            var result = await _forecastAIService.RejectAsync(rejectForecastAIDto.IdProduct, rejectForecastAIDto.IdCenter, rejectForecastAIDto.MonthYear, cancellationToken);
            return Ok(result);
        }
    }
}
