using System.ComponentModel.DataAnnotations;
using Service.Domain.Enums;

namespace Service.Application.DTOs.BufferProfile
{
    public class BufferProfilePostDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        [MaxLength(5, ErrorMessage = "The field {0} has the maxlength 5")]
        public string ProfileName { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [EnumDataType(typeof(SupplyType), ErrorMessage = "Invalid value for field {0}.")]
        public SupplyType SupplyType { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [EnumDataType(typeof(LeadTimeCategory), ErrorMessage = "Invalid value for field {0}.")]
        public LeadTimeCategory LeadTimeCategory { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [EnumDataType(typeof(VariabilityCategory), ErrorMessage = "Invalid value for field {0}.")]
        public VariabilityCategory VariabilityCategory { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Field {0} must be zero or greater.")]
        public decimal LeadTimeFactor { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Field {0} must be zero or greater.")]
        public decimal VariabilityFactor { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public int AduCalculationDays { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public int AduFutureDays { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public int Frequency { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public bool GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public bool GreenZoneParametrizationUseMoq { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public bool GreenZoneParametrizationUseAduXFrequency { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [EnumDataType(typeof(SpikeHorizonType), ErrorMessage = "Invalid value for field {0}.")]
        public SpikeHorizonType SpikeHorizonType { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public int SpikeHorizonValue { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public int SpikeHorizonLTDays { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [EnumDataType(typeof(SpikeThresholdType), ErrorMessage = "Invalid value for field {0}.")]
        public SpikeThresholdType SpikeThresholdType { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public int SpikeThresholdAdu { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Field {0} must be zero or greater.")]
        public decimal SpikeThresholdPercentageRedZone { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public bool IsActive { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public bool IsMakeToOrder { get; set; }
    }
}
