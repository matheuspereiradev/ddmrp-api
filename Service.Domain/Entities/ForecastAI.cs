using System;
using Service.Domain.Enums;

namespace Service.Domain.Entities
{
    public class ForecastAI : BaseEntity
    {
        public int IdCenter { get; set; }
        public Center Center { get; set; }
        public int IdProduct { get; set; }
        public Product Product { get; set; }
        public DateTime MonthYear { get; set; }
        public ForecastPreviewType PreviewType { get; set; }
        public decimal Assertiveness { get; set; }
        public decimal QuantityPreviewed { get; set; }
        public ForecastPreviewState PreviewState { get; set; } = ForecastPreviewState.NotReviewed;
    }
}
