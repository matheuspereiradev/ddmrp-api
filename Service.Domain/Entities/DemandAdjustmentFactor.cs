using System;
using Service.Domain.Enums;

namespace Service.Domain.Entities
{
    public class DemandAdjustmentFactor : BaseEntity
    {
        public int IdProduct { get; set; }
        public Product Product { get; set; }
        public int IdCenter { get; set; }
        public Center Center { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime EffectiveTo { get; set; }
        public bool IsActive { get; set; }
        public string? Obs { get; set; }
        public AdjustmentType AdjustmentType { get; set; }
        public decimal AdjustmentValue { get; set; }
    }
}
