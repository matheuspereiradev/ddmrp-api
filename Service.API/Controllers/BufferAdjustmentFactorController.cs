using Service.API.Extensions;
using Service.API.Filters;
using Service.API.Models;
using Service.Application.DTOs.BufferAdjustmentFactor;
using Service.Application.DTOs.Common;
using Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BufferAdjustmentFactorController : Controller
    {
        private readonly IBufferAdjustmentFactorService _bufferAdjustmentFactorService;

        public BufferAdjustmentFactorController(IBufferAdjustmentFactorService bufferAdjustmentFactorService)
        {
            _bufferAdjustmentFactorService = bufferAdjustmentFactorService;
        }

        [HttpPost]
        [Authorize]
        [RequirePermission]
        public async Task<ActionResult> CreateBufferAdjustmentFactor(BufferAdjustmentFactorPostDto bufferAdjustmentFactorPostDto, CancellationToken cancellationToken)
        {
            var bufferAdjustmentFactor = await _bufferAdjustmentFactorService.AddAsync(bufferAdjustmentFactorPostDto, cancellationToken);
            return Ok(bufferAdjustmentFactor);
        }

        [HttpGet]
        [Authorize]
        [RequirePermission]
        public async Task<ActionResult> GetAllBufferAdjustmentFactors(
            [FromQuery] int? idProduct,
            [FromQuery] int? idCenter,
            [FromQuery] PaginationParams paginationParams,
            CancellationToken cancellationToken)
        {
            var bufferAdjustmentFactors = await _bufferAdjustmentFactorService.GetFilteredAsync(idProduct, idCenter, paginationParams.PageNumber, paginationParams.PageSize, cancellationToken);

            Response.AddPaginationHeader(
                new PaginationHeader(paginationParams.PageNumber, paginationParams.PageSize, bufferAdjustmentFactors.TotalCount, bufferAdjustmentFactors.TotalPages));

            return Ok(bufferAdjustmentFactors);
        }

        [HttpPut("{id}")]
        [Authorize]
        [RequirePermission]
        public async Task<ActionResult> UpdateBufferAdjustmentFactor(int id, BufferAdjustmentFactorPutDto bufferAdjustmentFactorPutDto, CancellationToken cancellationToken)
        {
            var bufferAdjustmentFactor = await _bufferAdjustmentFactorService.UpdateAsync(id, bufferAdjustmentFactorPutDto, cancellationToken);
            return Ok(bufferAdjustmentFactor);
        }

        [HttpDelete("{id}")]
        [Authorize]
        [RequirePermission]
        public async Task<ActionResult> DeleteBufferAdjustmentFactor(int id, CancellationToken cancellationToken)
        {
            var bufferAdjustmentFactor = await _bufferAdjustmentFactorService.DeleteAsync(id, cancellationToken);
            return Ok(bufferAdjustmentFactor);
        }

        [HttpPatch("{id}/active")]
        [Authorize]
        [RequirePermission]
        public async Task<ActionResult> SetActive(int id, SetActiveDto setActiveDto, CancellationToken cancellationToken)
        {
            var bufferAdjustmentFactor = await _bufferAdjustmentFactorService.SetActiveAsync(id, setActiveDto.IsActive, cancellationToken);
            return Ok(bufferAdjustmentFactor);
        }
    }
}
