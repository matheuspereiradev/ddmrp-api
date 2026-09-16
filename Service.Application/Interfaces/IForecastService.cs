using Service.Application.DTOs.Forecast;
using Service.Domain.Entities;
using Service.Domain.Pagination;

namespace Service.Application.Interfaces
{
    public interface IForecastService : IBaseService<Forecast, ForecastGetDto, ForecastPostDto, ForecastPutDto>
    {
        Task<PagedList<ForecastDailyGetDto>> GetFilteredAsync(int? idProduct, int? idCenter, DateTime? dateStart, DateTime? dateEnd, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<PagedList<ForecastGetDto>> GetGroupedAsync(int? idProduct, int? idCenter, DateTime? dateStart, DateTime? dateEnd, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    }
}
