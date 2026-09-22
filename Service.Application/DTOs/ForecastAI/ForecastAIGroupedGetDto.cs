using System.Collections.Generic;
using Service.Application.DTOs.Center;
using Service.Application.DTOs.Product;

namespace Service.Application.DTOs.ForecastAI
{
    public class ForecastAIGroupedGetDto
    {
        public CenterGetDto? Center { get; set; }
        public ProductGetDto? Product { get; set; }
        public List<ForecastAIPreviewItemDto> Previews { get; set; } = new();
    }
}
