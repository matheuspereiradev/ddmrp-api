using System.ComponentModel.DataAnnotations;
using Service.Domain.Enums;

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
        public int? IdBufferProfile { get; set; }
        public int? FutureAduDays { get; set; }
        public int? HistoryAduDays { get; set; }
        public bool UseSuggestedLTFactor { get; set; } = true;
        public bool UseSuggestedVariabilityFactor { get; set; } = true;
        public bool UseDafOnGreenZone { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Field {0} must be zero or greater.")]
        public decimal CustomLeadTimeFactor { get; set; } = 1;

        [Range(0, double.MaxValue, ErrorMessage = "Field {0} must be zero or greater.")]
        public decimal CustomVariabilityFactor { get; set; } = 1;

        public bool GreenZoneParametrizationUseMoq { get; set; } = true;
        public bool GreenZoneParametrizationUseAduXFrequency { get; set; } = true;
        public bool GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime { get; set; } = true;

        [EnumDataType(typeof(BufferType), ErrorMessage = "Invalid value for field {0}.")]
        public BufferType BufferType { get; set; } = BufferType.Normal;
    }
}
