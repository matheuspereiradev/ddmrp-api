using System;
using Service.Domain.Enums;

namespace Service.Application.DTOs.ForecastAI
{
    public class ForecastAIPreviewItemDto
    {
        public int Id { get; set; }
        public DateTime MonthYear { get; set; }
        public ForecastPreviewType PreviewType { get; set; }
        public decimal Assertiveness { get; set; }
        public decimal QuantityPreviewed { get; set; }
        public ForecastPreviewState PreviewState { get; set; }
    }
}
