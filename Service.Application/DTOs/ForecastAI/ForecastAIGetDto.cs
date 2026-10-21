using System;
using Service.Application.DTOs.Center;
using Service.Application.DTOs.Product;
using Service.Domain.Enums;

namespace Service.Application.DTOs.ForecastAI
{
    public class ForecastAIGetDto
    {
        public int Id { get; set; }
        public int IdCenter { get; set; }
        public int IdProduct { get; set; }
        public DateTime MonthYear { get; set; }
        public ForecastPreviewType PreviewType { get; set; }
        public decimal Assertiveness { get; set; }
        public decimal QuantityPreviewed { get; set; }
        public ForecastPreviewState PreviewState { get; set; }
        public CenterGetDto? Center { get; set; }
        public ProductGetDto? Product { get; set; }
    }
}
