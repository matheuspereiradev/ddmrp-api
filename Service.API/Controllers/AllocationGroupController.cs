using Service.API.Extensions;
using Service.API.Models;
using Service.Application.DTOs.AllocationGroup;
using Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AllocationGroupController : Controller
    {
        private readonly IAllocationGroupService _allocationGroupService;

        public AllocationGroupController(IAllocationGroupService allocationGroupService)
        {
            _allocationGroupService = allocationGroupService;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult> CreateAllocationGroup(AllocationGroupPostDto allocationGroupPostDto, CancellationToken cancellationToken)
        {
            var allocationGroup = await _allocationGroupService.AddAsync(allocationGroupPostDto, cancellationToken);
            return Ok(allocationGroup);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> GetAllAllocationGroups([FromQuery] PaginationParams paginationParams, CancellationToken cancellationToken)
        {
            var allocationGroups = await _allocationGroupService.GetAllAsync(paginationParams.PageNumber, paginationParams.PageSize, cancellationToken);

            Response.AddPaginationHeader(
                new PaginationHeader(paginationParams.PageNumber, paginationParams.PageSize, allocationGroups.TotalCount, allocationGroups.TotalPages));

            return Ok(allocationGroups);
        }

        [HttpGet("priorizedAllocation")]
        [Authorize]
        public async Task<ActionResult> GetPriorizedAllocation(CancellationToken cancellationToken)
        {
            var result = await _allocationGroupService.GetPriorizedAllocationAsync(cancellationToken);
            return Ok(result);
        }

        [HttpPost("priorizedAllocation/run")]
        [Authorize]
        public async Task<ActionResult> RunPriorizedAllocation(PriorizedAllocationRunDto priorizedAllocationRunDto, CancellationToken cancellationToken)
        {
            var result = await _allocationGroupService.RunPriorizedAllocationAsync(priorizedAllocationRunDto, cancellationToken);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult> UpdateAllocationGroup(int id, AllocationGroupPutDto allocationGroupPutDto, CancellationToken cancellationToken)
        {
            var allocationGroup = await _allocationGroupService.UpdateAsync(id, allocationGroupPutDto, cancellationToken);
            return Ok(allocationGroup);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteAllocationGroup(int id, CancellationToken cancellationToken)
        {
            var allocationGroup = await _allocationGroupService.DeleteAsync(id, cancellationToken);
            return Ok(allocationGroup);
        }
    }
}
