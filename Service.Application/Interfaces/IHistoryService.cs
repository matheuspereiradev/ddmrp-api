using Service.Application.DTOs.History;
using Service.Domain.Entities;

namespace Service.Application.Interfaces
{
    public interface IHistoryService : IBaseService<History, HistoryGetDto, HistoryPostDto, HistoryPutDto>
    {
    }
}
