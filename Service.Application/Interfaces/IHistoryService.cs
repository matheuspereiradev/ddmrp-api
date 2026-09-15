using Service.Application.DTOs.History;
using Service.Domain.Entities;
using Service.Domain.Enums;
using Service.Domain.Pagination;

namespace Service.Application.Interfaces
{
    public interface IHistoryService : IBaseService<History, HistoryGetDto, HistoryPostDto, HistoryPutDto>
    {
        Task<PagedList<HistoryGetDto>> GetFilteredAsync(int? idProduct, int? idCenter, DateTime? dateStart, DateTime? dateEnd, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<HistoryGetDto> SetDiscardStatusAsync(int id, DiscardStatus discardStatus, CancellationToken cancellationToken = default);
    }
}
