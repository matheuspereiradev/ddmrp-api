using Service.Domain.Entities;
using Service.Domain.Enums;
using Service.Domain.Pagination;

namespace Service.Domain.Interfaces
{
    public interface IForecastAIRepository : IBaseRepository<ForecastAI>
    {
        Task<List<ForecastAI>> GetFilteredAsync(int? idCenter, int? idProduct, DateTime? monthYear, ForecastPreviewType? previewType, ForecastPreviewState? previewState, CancellationToken cancellationToken = default);
        Task<List<ForecastAI>> GetNotReviewedBySameItemAsync(int idProduct, int idCenter, DateTime monthYear, int? excludeId = null, CancellationToken cancellationToken = default);
    }
}
