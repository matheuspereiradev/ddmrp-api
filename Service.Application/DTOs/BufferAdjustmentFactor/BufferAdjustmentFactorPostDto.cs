using System;
using System.ComponentModel.DataAnnotations;
using Service.Domain.Enums;

namespace Service.Application.DTOs.BufferAdjustmentFactor
{
    public class BufferAdjustmentFactorPostDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        public int IdProduct { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public int IdCenter { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public DateTime EffectiveFrom { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public DateTime EffectiveTo { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [EnumDataType(typeof(BufferType), ErrorMessage = "Invalid value for field {0}.")]
        public BufferType BufferType { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Field {0} must be zero or greater.")]
        public decimal BufferDdmrpRed { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Field {0} must be zero or greater.")]
        public decimal BufferDdmrpYellow { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Field {0} must be zero or greater.")]
        public decimal BufferDdmrpGreen { get; set; }

        [MaxLength(300, ErrorMessage = "The field {0} has the maxlength 300")]
        public string? Obs { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public bool IsActive { get; set; }
    }
}
