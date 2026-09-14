using System.ComponentModel.DataAnnotations;
using Service.Domain.Enums;

namespace Service.Application.DTOs.History
{
    public class HistoryPutDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Field {0} must be zero or greater.")]
        public decimal Consumption { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [EnumDataType(typeof(DiscardStatus), ErrorMessage = "Field {0} has an invalid value.")]
        public DiscardStatus DiscardStatus { get; set; }
    }
}
