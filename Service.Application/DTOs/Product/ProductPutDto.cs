using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.Product
{
    public class ProductPutDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        [MaxLength(50, ErrorMessage = "The field {0} has the maxlength 50")]
        public string Reference { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [MaxLength(200, ErrorMessage = "The field {0} has the maxlength 200")]
        public string Description { get; set; }

        [MaxLength(50, ErrorMessage = "The field {0} has the maxlength 50")]
        public string? AuxiliarMaterialCode { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [MaxLength(20, ErrorMessage = "The field {0} has the maxlength 20")]
        public string UnitOfMeasure { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Field {0} must be zero or greater.")]
        public decimal? Weight { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Field {0} must be zero or greater.")]
        public decimal? Volume { get; set; }

        [MaxLength(50, ErrorMessage = "The field {0} has the maxlength 50")]
        public string? Barcode { get; set; }

        [MaxLength(100, ErrorMessage = "The field {0} has the maxlength 100")]
        public string? Category { get; set; }

        [MaxLength(100, ErrorMessage = "The field {0} has the maxlength 100")]
        public string? Segment { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Field {0} must be zero or greater.")]
        public decimal? Value { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Field {0} must be zero or greater.")]
        public decimal? Pallet { get; set; }

        [MaxLength(100, ErrorMessage = "The field {0} has the maxlength 100")]
        public string? Line { get; set; }

        [MaxLength(100, ErrorMessage = "The field {0} has the maxlength 100")]
        public string? Subline { get; set; }

        [MaxLength(100, ErrorMessage = "The field {0} has the maxlength 100")]
        public string? Brand { get; set; }

        [MaxLength(50, ErrorMessage = "The field {0} has the maxlength 50")]
        public string? WorkCenter { get; set; }
    }
}
