using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.History
{
    public class HistoryPutDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Field {0} must be zero or greater.")]
        public decimal Quantity { get; set; }
    }
}
