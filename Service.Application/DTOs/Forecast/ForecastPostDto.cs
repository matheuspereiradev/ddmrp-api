using System;
using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.Forecast
{
    public class ForecastPostDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        public int IdProduct { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public int IdCenter { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Field {0} must be zero or greater.")]
        public decimal Value { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public DateTime EndDate { get; set; }
    }
}
