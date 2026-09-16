using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.Forecast
{
    public class ForecastPutDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Field {0} must be zero or greater.")]
        public decimal Value { get; set; }
    }
}
