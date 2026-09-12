using Service.API.Extensions;
using Service.API.Models;
using Service.Application.DTOs.BufferProfile;
using Service.Application.DTOs.Common;
using Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BufferProfileController : Controller
    {
        private readonly IBufferProfileService _bufferProfileService;

        public BufferProfileController(IBufferProfileService bufferProfileService)
        {
            _bufferProfileService = bufferProfileService;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult> CreateBufferProfile(BufferProfilePostDto bufferProfilePostDto, CancellationToken cancellationToken)
        {
            var bufferProfile = await _bufferProfileService.AddAsync(bufferProfilePostDto, cancellationToken);
            return Ok(bufferProfile);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> GetAllBufferProfiles([FromQuery] PaginationParams paginationParams, CancellationToken cancellationToken)
        {
            var bufferProfiles = await _bufferProfileService.GetAllAsync(paginationParams.PageNumber, paginationParams.PageSize, cancellationToken);

            Response.AddPaginationHeader(
                new PaginationHeader(paginationParams.PageNumber, paginationParams.PageSize, bufferProfiles.TotalCount, bufferProfiles.TotalPages));

            return Ok(bufferProfiles);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult> UpdateBufferProfile(int id, BufferProfilePutDto bufferProfilePutDto, CancellationToken cancellationToken)
        {
            var bufferProfile = await _bufferProfileService.UpdateAsync(id, bufferProfilePutDto, cancellationToken);
            return Ok(bufferProfile);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteBufferProfile(int id, CancellationToken cancellationToken)
        {
            var bufferProfile = await _bufferProfileService.DeleteAsync(id, cancellationToken);
            return Ok(bufferProfile);
        }

        [HttpPatch("{id}/active")]
        [Authorize]
        public async Task<ActionResult> SetActive(int id, SetActiveDto setActiveDto, CancellationToken cancellationToken)
        {
            var bufferProfile = await _bufferProfileService.SetActiveAsync(id, setActiveDto.IsActive, cancellationToken);
            return Ok(bufferProfile);
        }
    }
}
