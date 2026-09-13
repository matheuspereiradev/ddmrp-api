using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.Order
{
    public class OrderPutDto
    {
        public int? IdPartner { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Field {0} must be zero or greater.")]
        public decimal Quantity { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Field {0} must be zero or greater.")]
        public decimal DeliveredQuantity { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [MaxLength(20, ErrorMessage = "The field {0} has the maxlength 20")]
        public string MeasurementUnit { get; set; }

        public int? Position { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public DateTime CreationDate { get; set; }

        public DateTime? DeliveryDate { get; set; }

        [MaxLength(500, ErrorMessage = "The field {0} has the maxlength 500")]
        public string? Notes { get; set; }
    }
}
