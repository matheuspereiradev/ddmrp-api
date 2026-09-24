using Service.API.Extensions;
using Service.API.Filters;
using Service.API.Models;
using Service.Application.DTOs.Note;
using Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NoteController : Controller
    {
        private readonly INoteService _noteService;

        public NoteController(INoteService noteService)
        {
            _noteService = noteService;
        }

        [HttpPost]
        [Authorize]
        [RequirePermission]
        public async Task<ActionResult> CreateNote(NotePostDto notePostDto, CancellationToken cancellationToken)
        {
            var note = await _noteService.AddAsync(notePostDto, cancellationToken);
            return Ok(note);
        }

        [HttpGet]
        [Authorize]
        [RequirePermission]
        public async Task<ActionResult> GetAllNotes([FromQuery] int? idCenterProduct, [FromQuery] PaginationParams paginationParams, CancellationToken cancellationToken)
        {
            var notes = idCenterProduct.HasValue
                ? await _noteService.GetByCenterProductAsync(idCenterProduct.Value, paginationParams.PageNumber, paginationParams.PageSize, cancellationToken)
                : await _noteService.GetAllAsync(paginationParams.PageNumber, paginationParams.PageSize, cancellationToken);

            Response.AddPaginationHeader(
                new PaginationHeader(paginationParams.PageNumber, paginationParams.PageSize, notes.TotalCount, notes.TotalPages));

            return Ok(notes);
        }

        [HttpPut("{id}")]
        [Authorize]
        [RequirePermission]
        public async Task<ActionResult> UpdateNote(int id, NotePutDto notePutDto, CancellationToken cancellationToken)
        {
            var note = await _noteService.UpdateAsync(id, notePutDto, cancellationToken);
            return Ok(note);
        }

        [HttpDelete("{id}")]
        [Authorize]
        [RequirePermission]
        public async Task<ActionResult> DeleteNote(int id, CancellationToken cancellationToken)
        {
            var note = await _noteService.DeleteAsync(id, cancellationToken);
            return Ok(note);
        }
    }
}
