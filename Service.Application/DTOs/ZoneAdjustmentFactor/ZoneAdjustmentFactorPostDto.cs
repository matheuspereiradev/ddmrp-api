using System;
using System.ComponentModel.DataAnnotations;
using Service.Domain.Enums;

namespace Service.Application.DTOs.ZoneAdjustmentFactor
{
    public class ZoneAdjustmentFactorPostDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        public int IdProduct { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public int IdCenter { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [EnumDataType(typeof(TargetZone), ErrorMessage = "Invalid value for field {0}.")]
        public TargetZone TargetZone { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [EnumDataType(typeof(AdjustmentType), ErrorMessage = "Invalid value for field {0}.")]
        public AdjustmentType AdjustmentType { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Field {0} must be zero or greater.")]
        public decimal AdjustmentValue { get; set; }

        [MaxLength(300, ErrorMessage = "The field {0} has the maxlength 300")]
        public string? Obs { get; set; }

        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "Field {0} required.")]
        public DateTime EffectiveFrom { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public DateTime EffectiveTo { get; set; }
    }
}
