using System;
using Service.Application.DTOs.Center;
using Service.Application.DTOs.Product;

namespace Service.Application.DTOs.Forecast
{
    public class ForecastDailyGetDto
    {
        public int Id { get; set; }
        public int IdProduct { get; set; }
        public int IdCenter { get; set; }
        public decimal Value { get; set; }
        public DateTime Date { get; set; }
        public ProductGetDto? Product { get; set; }
        public CenterGetDto? Center { get; set; }
    }
}
