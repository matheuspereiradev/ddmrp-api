using System;
using Service.Application.DTOs.Center;
using Service.Application.DTOs.Product;
using Service.Domain.Enums;

namespace Service.Application.DTOs.BufferAdjustmentFactor
{
    public class BufferAdjustmentFactorGetDto
    {
        public int Id { get; set; }
        public int IdProduct { get; set; }
        public int IdCenter { get; set; }
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
        public bool AlreadyReverted { get; set; }
        public ProductGetDto? Product { get; set; }
        public CenterGetDto? Center { get; set; }
    }
}
