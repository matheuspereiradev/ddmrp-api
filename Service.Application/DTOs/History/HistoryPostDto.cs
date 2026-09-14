using System;
using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.History
{
    public class HistoryPostDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        public int IdProduct { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public int IdCenter { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Field {0} must be zero or greater.")]
        public decimal Consumption { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public DateTime Date { get; set; }
    }
}
