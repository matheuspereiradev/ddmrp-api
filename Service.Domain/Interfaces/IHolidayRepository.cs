using Service.Domain.Entities;
using Service.Domain.Pagination;

namespace Service.Domain.Interfaces
{
    public interface IHolidayRepository : IBaseRepository<Holiday>
    {
        Task<List<Holiday>> AddRangeAsync(List<Holiday> holidays, CancellationToken cancellationToken = default);
        Task<PagedList<Holiday>> GetFilteredAsync(int pageNumber, int pageSize, bool includePast, CancellationToken cancellationToken = default);
    }
}
