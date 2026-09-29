using Service.Application.DTOs.ReasonGroup;
using Service.Domain.Pagination;

namespace Service.Application.Interfaces
{
    public interface IReasonGroupService
    {
        Task<PagedList<ReasonGroupGetDto>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    }
}
