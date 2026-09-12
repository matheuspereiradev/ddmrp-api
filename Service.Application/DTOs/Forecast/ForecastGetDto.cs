using System;

namespace Service.Application.DTOs.Forecast
{
    public class ForecastGetDto
    {
        public int Id { get; set; }
        public int IdProduct { get; set; }
        public int IdCenter { get; set; }
        public decimal Quantity { get; set; }
        public DateTime Date { get; set; }
    }
}
