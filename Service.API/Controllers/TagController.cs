using Service.API.Extensions;
using Service.API.Models;
using Service.Application.DTOs.Tag;
using Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TagController : Controller
    {
        private readonly ITagService _tagService;

        public TagController(ITagService tagService)
        {
            _tagService = tagService;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult> CreateTag(TagPostDto tagPostDto, CancellationToken cancellationToken)
        {
            var tag = await _tagService.AddAsync(tagPostDto, cancellationToken);
            return Ok(tag);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> GetAllTags([FromQuery] PaginationParams paginationParams, CancellationToken cancellationToken)
        {
            var tags = await _tagService.GetAllAsync(paginationParams.PageNumber, paginationParams.PageSize, cancellationToken);

            Response.AddPaginationHeader(
                new PaginationHeader(paginationParams.PageNumber, paginationParams.PageSize, tags.TotalCount, tags.TotalPages));

            return Ok(tags);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult> UpdateTag(int id, TagPutDto tagPutDto, CancellationToken cancellationToken)
        {
            var tag = await _tagService.UpdateAsync(id, tagPutDto, cancellationToken);
            return Ok(tag);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteTag(int id, CancellationToken cancellationToken)
        {
            var tag = await _tagService.DeleteAsync(id, cancellationToken);
            return Ok(tag);
        }
    }
}
