using Service.Domain.Entities;
using Service.Domain.Forecasts;
using Service.Domain.Pagination;

namespace Service.Domain.Interfaces
{
    public interface IForecastRepository : IBaseRepository<Forecast>
    {
        Task<PagedList<ForecastDailyRow>> GetFilteredAsync(int? idProduct, int? idCenter, DateTime? dateStart, DateTime? dateEnd, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<PagedList<Forecast>> GetGroupedAsync(int? idProduct, int? idCenter, DateTime? dateStart, DateTime? dateEnd, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    }
}
