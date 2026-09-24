using Service.API.Extensions;
using Service.API.Filters;
using Service.API.Models;
using Service.Application.DTOs.Center;
using Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CenterController : Controller
    {
        private readonly ICenterService _centerService;

        public CenterController(ICenterService centerService)
        {
            _centerService = centerService;
        }

        [HttpPost]
        [Authorize]
        [RequirePermission]
        public async Task<ActionResult> CreateCenter(CenterPostDto centerPostDto, CancellationToken cancellationToken)
        {
            var center = await _centerService.AddAsync(centerPostDto, cancellationToken);
            return Ok(center);
        }

        [HttpGet]
        [Authorize]
        [RequirePermission]
        public async Task<ActionResult> GetAllCenters([FromQuery] PaginationParams paginationParams, CancellationToken cancellationToken)
        {
            var centers = await _centerService.GetAllAsync(paginationParams.PageNumber, paginationParams.PageSize, cancellationToken);

            Response.AddPaginationHeader(
                new PaginationHeader(paginationParams.PageNumber, paginationParams.PageSize, centers.TotalCount, centers.TotalPages));

            return Ok(centers);
        }

        [HttpPut("{id}")]
        [Authorize]
        [RequirePermission]
        public async Task<ActionResult> UpdateCenter(int id, CenterPutDto centerPutDto, CancellationToken cancellationToken)
        {
            var center = await _centerService.UpdateAsync(id, centerPutDto, cancellationToken);
            return Ok(center);
        }

        [HttpDelete("{id}")]
        [Authorize]
        [RequirePermission]
        public async Task<ActionResult> DeleteCenter(int id, CancellationToken cancellationToken)
        {
            var center = await _centerService.DeleteAsync(id, cancellationToken);
            return Ok(center);
        }
    }
}
