using Service.Application.DTOs.Forecast;
using Service.Domain.Entities;

namespace Service.Application.Interfaces
{
    public interface IForecastService : IBaseService<Forecast, ForecastGetDto, ForecastPostDto, ForecastPutDto>
    {
    }
}
