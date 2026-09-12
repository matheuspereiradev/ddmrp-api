using Service.API.Extensions;
using Service.API.Models;
using Service.Application.DTOs.Common;
using Service.Application.DTOs.DemandAdjustmentFactor;
using Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DemandAdjustmentFactorController : Controller
    {
        private readonly IDemandAdjustmentFactorService _demandAdjustmentFactorService;

        public DemandAdjustmentFactorController(IDemandAdjustmentFactorService demandAdjustmentFactorService)
        {
            _demandAdjustmentFactorService = demandAdjustmentFactorService;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult> CreateDemandAdjustmentFactor(DemandAdjustmentFactorPostDto demandAdjustmentFactorPostDto, CancellationToken cancellationToken)
        {
            var demandAdjustmentFactor = await _demandAdjustmentFactorService.AddAsync(demandAdjustmentFactorPostDto, cancellationToken);
            return Ok(demandAdjustmentFactor);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> GetAllDemandAdjustmentFactors([FromQuery] PaginationParams paginationParams, CancellationToken cancellationToken)
        {
            var demandAdjustmentFactors = await _demandAdjustmentFactorService.GetAllAsync(paginationParams.PageNumber, paginationParams.PageSize, cancellationToken);

            Response.AddPaginationHeader(
                new PaginationHeader(paginationParams.PageNumber, paginationParams.PageSize, demandAdjustmentFactors.TotalCount, demandAdjustmentFactors.TotalPages));

            return Ok(demandAdjustmentFactors);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult> UpdateDemandAdjustmentFactor(int id, DemandAdjustmentFactorPutDto demandAdjustmentFactorPutDto, CancellationToken cancellationToken)
        {
            var demandAdjustmentFactor = await _demandAdjustmentFactorService.UpdateAsync(id, demandAdjustmentFactorPutDto, cancellationToken);
            return Ok(demandAdjustmentFactor);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteDemandAdjustmentFactor(int id, CancellationToken cancellationToken)
        {
            var demandAdjustmentFactor = await _demandAdjustmentFactorService.DeleteAsync(id, cancellationToken);
            return Ok(demandAdjustmentFactor);
        }

        [HttpPatch("{id}/active")]
        [Authorize]
        public async Task<ActionResult> SetActive(int id, SetActiveDto setActiveDto, CancellationToken cancellationToken)
        {
            var demandAdjustmentFactor = await _demandAdjustmentFactorService.SetActiveAsync(id, setActiveDto.IsActive, cancellationToken);
            return Ok(demandAdjustmentFactor);
        }
    }
}
