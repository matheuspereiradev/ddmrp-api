using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.CenterProduct
{
    public class CenterProductPutDto
    {
        public int? IdOriginCenter { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Field {0} must be zero or greater.")]
        public decimal PackQuantity { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Field {0} must be zero or greater.")]
        public decimal Moq { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public int LeadTime { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public int Frequency { get; set; }

        [MaxLength(200, ErrorMessage = "The field {0} has the maxlength 200")]
        public string? Class { get; set; }

        [MaxLength(200, ErrorMessage = "The field {0} has the maxlength 200")]
        public string? Classification { get; set; }

        [MaxLength(200, ErrorMessage = "The field {0} has the maxlength 200")]
        public string? Segment { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Field {0} must be zero or greater.")]
        public decimal Stock { get; set; }

        public int? IdProvider { get; set; }
        public int? IdTag { get; set; }
        public int? IdReason { get; set; }
        public int? IdAllocationGroup { get; set; }
    }
}
