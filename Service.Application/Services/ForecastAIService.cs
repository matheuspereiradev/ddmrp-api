using Service.Application.DTOs.Forecast;
using Service.Application.DTOs.ForecastAI;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Application.Mappers;
using Service.Domain.Entities;
using Service.Domain.Enums;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;

namespace Service.Application.Services
{
    public class ForecastAIService : IForecastAIService
    {
        private readonly IForecastAIRepository _forecastAIRepository;
        private readonly IForecastRepository _forecastRepository;

        public ForecastAIService(IForecastAIRepository forecastAIRepository, IForecastRepository forecastRepository)
        {
            _forecastAIRepository = forecastAIRepository;
            _forecastRepository = forecastRepository;
        }

        public async Task<PagedList<ForecastAIGroupedGetDto>> GetFilteredAsync(int? idCenter, int? idProduct, DateTime? monthYear, ForecastPreviewType? previewType, ForecastPreviewState? previewState, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var previews = await _forecastAIRepository.GetFilteredAsync(idCenter, idProduct, monthYear, previewType, previewState, cancellationToken);

            var groups = previews
                .GroupBy(f => (f.IdCenter, f.IdProduct))
                .Select(g => new ForecastAIGroupedGetDto
                {
                    Center = g.First().Center?.ToGetDto(),
                    Product = g.First().Product?.ToGetDto(),
                    Previews = g.Select(ToPreviewItemDTO).ToList()
                })
                .ToList();

            var totalCount = groups.Count;
            var paged = groups.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            return new PagedList<ForecastAIGroupedGetDto>(paged, pageNumber, pageSize, totalCount);
        }

        public async Task<ForecastAIApplyResultDto> ApplyAsync(int id, CancellationToken cancellationToken = default)
        {
            var forecastAi = await _forecastAIRepository.GetByIdAsync(id, cancellationToken);
            if (forecastAi == null)
                throw new NotFoundException("Not found");

            if (forecastAi.PreviewState != ForecastPreviewState.NotReviewed)
                throw new BadRequestException("This AI forecast preview has already been reviewed.");

            var startDate = new DateTime(forecastAi.MonthYear.Year, forecastAi.MonthYear.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var existingForecast = await _forecastRepository.GetByProductCenterAndPeriodAsync(forecastAi.IdProduct, forecastAi.IdCenter, startDate, endDate, cancellationToken);

            Forecast forecast;
            if (existingForecast != null)
            {
                existingForecast.Value = forecastAi.QuantityPreviewed;
                forecast = await _forecastRepository.UpdateAsync(existingForecast, cancellationToken);
            }
            else
            {
                forecast = await _forecastRepository.AddAsync(new Forecast
                {
                    IdProduct = forecastAi.IdProduct,
                    IdCenter = forecastAi.IdCenter,
                    Value = forecastAi.QuantityPreviewed,
                    StartDate = startDate,
                    EndDate = endDate
                }, cancellationToken);
            }

            forecastAi.PreviewState = ForecastPreviewState.Applied;
            var appliedPreview = await _forecastAIRepository.UpdateAsync(forecastAi, cancellationToken);

            var others = await _forecastAIRepository.GetNotReviewedBySameItemAsync(forecastAi.IdProduct, forecastAi.IdCenter, forecastAi.MonthYear, forecastAi.Id, cancellationToken);
            foreach (var other in others)
            {
                other.PreviewState = ForecastPreviewState.Discarded;
                await _forecastAIRepository.UpdateAsync(other, cancellationToken);
            }

            return new ForecastAIApplyResultDto
            {
                AppliedPreview = ToGetDTO(appliedPreview),
                Forecast = ToForecastGetDTO(forecast)
            };
        }

        public async Task<List<ForecastAIGetDto>> RejectAsync(int idProduct, int idCenter, DateTime monthYear, CancellationToken cancellationToken = default)
        {
            var previews = await _forecastAIRepository.GetNotReviewedBySameItemAsync(idProduct, idCenter, monthYear, null, cancellationToken);

            var rejected = new List<ForecastAIGetDto>();
            foreach (var preview in previews)
            {
                preview.PreviewState = ForecastPreviewState.Discarded;
                var updated = await _forecastAIRepository.UpdateAsync(preview, cancellationToken);
                rejected.Add(ToGetDTO(updated));
            }

            return rejected;
        }

        private static ForecastAIPreviewItemDto ToPreviewItemDTO(ForecastAI entity) => new()
        {
            Id = entity.Id,
            MonthYear = entity.MonthYear,
            PreviewType = entity.PreviewType,
            Assertiveness = entity.Assertiveness,
            QuantityPreviewed = entity.QuantityPreviewed,
            PreviewState = entity.PreviewState
        };

        private static ForecastAIGetDto ToGetDTO(ForecastAI entity) => new()
        {
            Id = entity.Id,
            IdCenter = entity.IdCenter,
            IdProduct = entity.IdProduct,
            MonthYear = entity.MonthYear,
            PreviewType = entity.PreviewType,
            Assertiveness = entity.Assertiveness,
            QuantityPreviewed = entity.QuantityPreviewed,
            PreviewState = entity.PreviewState,
            Center = entity.Center?.ToGetDto(),
            Product = entity.Product?.ToGetDto()
        };

        private static ForecastGetDto ToForecastGetDTO(Forecast entity) => new()
        {
            Id = entity.Id,
            IdProduct = entity.IdProduct,
            IdCenter = entity.IdCenter,
            Value = entity.Value,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Product = entity.Product?.ToGetDto(),
            Center = entity.Center?.ToGetDto()
        };
    }
}
