using System;
using Service.Domain.Entities;

namespace Service.Domain.Forecasts
{
    public class ForecastDailyRow
    {
        public int Id { get; set; }
        public int IdProduct { get; set; }
        public int IdCenter { get; set; }
        public decimal Value { get; set; }
        public DateTime Date { get; set; }
        public Product? Product { get; set; }
        public Center? Center { get; set; }
    }
}
