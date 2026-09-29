using Service.API.Extensions;
using Service.API.Filters;
using Service.API.Models;
using Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReasonGroupController : Controller
    {
        private readonly IReasonGroupService _reasonGroupService;

        public ReasonGroupController(IReasonGroupService reasonGroupService)
        {
            _reasonGroupService = reasonGroupService;
        }

        [HttpGet]
        [Authorize]
        [RequirePermission]
        public async Task<ActionResult> GetAllReasonGroups([FromQuery] PaginationParams paginationParams, CancellationToken cancellationToken)
        {
            var reasonGroups = await _reasonGroupService.GetAllAsync(paginationParams.PageNumber, paginationParams.PageSize, cancellationToken);

            Response.AddPaginationHeader(
                new PaginationHeader(paginationParams.PageNumber, paginationParams.PageSize, reasonGroups.TotalCount, reasonGroups.TotalPages));

            return Ok(reasonGroups);
        }
    }
}
