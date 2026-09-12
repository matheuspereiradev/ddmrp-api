using Service.API.Extensions;
using Service.API.Models;
using Service.Application.DTOs.Common;
using Service.Application.DTOs.ZoneAdjustmentFactor;
using Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ZoneAdjustmentFactorController : Controller
    {
        private readonly IZoneAdjustmentFactorService _zoneAdjustmentFactorService;

        public ZoneAdjustmentFactorController(IZoneAdjustmentFactorService zoneAdjustmentFactorService)
        {
            _zoneAdjustmentFactorService = zoneAdjustmentFactorService;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult> CreateZoneAdjustmentFactor(ZoneAdjustmentFactorPostDto zoneAdjustmentFactorPostDto, CancellationToken cancellationToken)
        {
            var zoneAdjustmentFactor = await _zoneAdjustmentFactorService.AddAsync(zoneAdjustmentFactorPostDto, cancellationToken);
            return Ok(zoneAdjustmentFactor);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> GetAllZoneAdjustmentFactors([FromQuery] PaginationParams paginationParams, CancellationToken cancellationToken)
        {
            var zoneAdjustmentFactors = await _zoneAdjustmentFactorService.GetAllAsync(paginationParams.PageNumber, paginationParams.PageSize, cancellationToken);

            Response.AddPaginationHeader(
                new PaginationHeader(paginationParams.PageNumber, paginationParams.PageSize, zoneAdjustmentFactors.TotalCount, zoneAdjustmentFactors.TotalPages));

            return Ok(zoneAdjustmentFactors);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult> UpdateZoneAdjustmentFactor(int id, ZoneAdjustmentFactorPutDto zoneAdjustmentFactorPutDto, CancellationToken cancellationToken)
        {
            var zoneAdjustmentFactor = await _zoneAdjustmentFactorService.UpdateAsync(id, zoneAdjustmentFactorPutDto, cancellationToken);
            return Ok(zoneAdjustmentFactor);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteZoneAdjustmentFactor(int id, CancellationToken cancellationToken)
        {
            var zoneAdjustmentFactor = await _zoneAdjustmentFactorService.DeleteAsync(id, cancellationToken);
            return Ok(zoneAdjustmentFactor);
        }

        [HttpPatch("{id}/active")]
        [Authorize]
        public async Task<ActionResult> SetActive(int id, SetActiveDto setActiveDto, CancellationToken cancellationToken)
        {
            var zoneAdjustmentFactor = await _zoneAdjustmentFactorService.SetActiveAsync(id, setActiveDto.IsActive, cancellationToken);
            return Ok(zoneAdjustmentFactor);
        }
    }
}
