using Service.Application.DTOs.ForecastAI;
using Service.Domain.Enums;
using Service.Domain.Pagination;

namespace Service.Application.Interfaces
{
    public interface IForecastAIService
    {
        Task<PagedList<ForecastAIGroupedGetDto>> GetFilteredAsync(int? idCenter, int? idProduct, DateTime? monthYear, ForecastPreviewType? previewType, ForecastPreviewState? previewState, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<ForecastAIApplyResultDto> ApplyAsync(int id, CancellationToken cancellationToken = default);
        Task<List<ForecastAIGetDto>> RejectAsync(int idProduct, int idCenter, DateTime monthYear, CancellationToken cancellationToken = default);
    }
}
