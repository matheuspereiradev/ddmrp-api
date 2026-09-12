using Service.Application.DTOs.Note;
using Service.Domain.Entities;

namespace Service.Application.Interfaces
{
    public interface INoteService : IBaseService<Note, NoteGetDto, NotePostDto, NotePutDto>
    {
    }
}
