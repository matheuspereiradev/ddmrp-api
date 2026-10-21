using System;
using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.ForecastAI
{
    public class RejectForecastAIDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        public int IdProduct { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public int IdCenter { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public DateTime MonthYear { get; set; }
    }
}
