using System;
using Service.Domain.Enums;

namespace Service.Domain.Entities
{
    public class BufferAdjustmentFactor : BaseEntity
    {
        public int IdProduct { get; set; }
        public Product Product { get; set; }
        public int IdCenter { get; set; }
        public Center Center { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime EffectiveTo { get; set; }
        public BufferType BufferType { get; set; }
        public decimal BufferDdmrpRed { get; set; }
        public decimal BufferDdmrpYellow { get; set; }
        public decimal BufferDdmrpGreen { get; set; }
        public string? Obs { get; set; }
        public bool IsActive { get; set; }
        public BufferType? BufferTypeOld { get; set; }
        public decimal? BufferDdmrpRedSafeOld { get; set; }
        public decimal? BufferDdmrpRedBaseOld { get; set; }
        public decimal? BufferDdmrpYellowOld { get; set; }
        public decimal? BufferDdmrpGreenOld { get; set; }
        public bool AlreadyReverted { get; set; } = false;
    }
}
