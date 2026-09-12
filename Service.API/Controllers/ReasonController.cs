using Service.API.Extensions;
using Service.API.Models;
using Service.Application.DTOs.Reason;
using Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReasonController : Controller
    {
        private readonly IReasonService _reasonService;

        public ReasonController(IReasonService reasonService)
        {
            _reasonService = reasonService;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult> CreateReason(ReasonPostDto reasonPostDto, CancellationToken cancellationToken)
        {
            var reason = await _reasonService.AddAsync(reasonPostDto, cancellationToken);
            return Ok(reason);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> GetAllReasons([FromQuery] PaginationParams paginationParams, CancellationToken cancellationToken)
        {
            var reasons = await _reasonService.GetAllAsync(paginationParams.PageNumber, paginationParams.PageSize, cancellationToken);

            Response.AddPaginationHeader(
                new PaginationHeader(paginationParams.PageNumber, paginationParams.PageSize, reasons.TotalCount, reasons.TotalPages));

            return Ok(reasons);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult> UpdateReason(int id, ReasonPutDto reasonPutDto, CancellationToken cancellationToken)
        {
            var reason = await _reasonService.UpdateAsync(id, reasonPutDto, cancellationToken);
            return Ok(reason);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteReason(int id, CancellationToken cancellationToken)
        {
            var reason = await _reasonService.DeleteAsync(id, cancellationToken);
            return Ok(reason);
        }
    }
}
