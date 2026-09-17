using System.ComponentModel.DataAnnotations;
using Service.Domain.Enums;

namespace Service.Application.DTOs.AllocationGroup
{
    public class PriorizedAllocationRunDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        public int IdGroup { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Field {0} must be zero or greater.")]
        public decimal Limit { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [EnumDataType(typeof(PriorizedAllocationStopCondition))]
        public PriorizedAllocationStopCondition StopCondition { get; set; }

        [EnumDataType(typeof(PriorizedAllocationAdjustmentType))]
        public PriorizedAllocationAdjustmentType AdjustmentType { get; set; } = PriorizedAllocationAdjustmentType.Unit;
    }
}
