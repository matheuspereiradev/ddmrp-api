using Service.API.Extensions;
using Service.API.Models;
using Service.Application.DTOs.MasterBuffer;
using Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MasterBufferController : Controller
    {
        private readonly IMasterBufferService _masterBufferService;

        public MasterBufferController(IMasterBufferService masterBufferService)
        {
            _masterBufferService = masterBufferService;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult> CreateMasterBuffer(MasterBufferPostDto masterBufferPostDto, CancellationToken cancellationToken)
        {
            var masterBuffer = await _masterBufferService.AddAsync(masterBufferPostDto, cancellationToken);
            return Ok(masterBuffer);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> GetAllMasterBuffers([FromQuery] PaginationParams paginationParams, CancellationToken cancellationToken)
        {
            var masterBuffers = await _masterBufferService.GetAllAsync(paginationParams.PageNumber, paginationParams.PageSize, cancellationToken);

            Response.AddPaginationHeader(
                new PaginationHeader(paginationParams.PageNumber, paginationParams.PageSize, masterBuffers.TotalCount, masterBuffers.TotalPages));

            return Ok(masterBuffers);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult> UpdateMasterBuffer(int id, MasterBufferPutDto masterBufferPutDto, CancellationToken cancellationToken)
        {
            var masterBuffer = await _masterBufferService.UpdateAsync(id, masterBufferPutDto, cancellationToken);
            return Ok(masterBuffer);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteMasterBuffer(int id, CancellationToken cancellationToken)
        {
            var masterBuffer = await _masterBufferService.DeleteAsync(id, cancellationToken);
            return Ok(masterBuffer);
        }
    }
}
