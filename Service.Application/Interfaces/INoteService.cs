using Service.Application.DTOs.Note;
using Service.Domain.Entities;
using Service.Domain.Pagination;

namespace Service.Application.Interfaces
{
    public interface INoteService : IBaseService<Note, NoteGetDto, NotePostDto, NotePutDto>
    {
        Task<PagedList<NoteGetDto>> GetByCenterProductAsync(int idCenterProduct, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    }
}
