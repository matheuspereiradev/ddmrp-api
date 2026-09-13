using System.ComponentModel.DataAnnotations;
using Service.Domain.Enums;

namespace Service.Application.DTOs.Order
{
    public class OrderPostDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        [MaxLength(50, ErrorMessage = "The field {0} has the maxlength 50")]
        public string OrderNumber { get; set; }

        public int? IdPartner { get; set; }
        public int? IdDestinyCenter { get; set; }
        public int? IdOriginCenter { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public int IdProduct { get; set; }

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

        [Required(ErrorMessage = "Field {0} required.")]
        [EnumDataType(typeof(OrderType), ErrorMessage = "Invalid value for field {0}.")]
        public OrderType Type { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public bool IsInbound { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public bool IsOutbound { get; set; }

        public bool IsFictional { get; set; } = false;
    }
}
