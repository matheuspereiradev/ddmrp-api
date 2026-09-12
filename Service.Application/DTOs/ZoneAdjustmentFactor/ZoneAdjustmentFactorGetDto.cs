using System;
using Service.Application.DTOs.Center;
using Service.Application.DTOs.Product;
using Service.Domain.Enums;

namespace Service.Application.DTOs.ZoneAdjustmentFactor
{
    public class ZoneAdjustmentFactorGetDto
    {
        public int Id { get; set; }
        public int IdProduct { get; set; }
        public int IdCenter { get; set; }
        public TargetZone TargetZone { get; set; }
        public AdjustmentType AdjustmentType { get; set; }
        public decimal AdjustmentValue { get; set; }
        public string? Obs { get; set; }
        public bool IsActive { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime EffectiveTo { get; set; }
        public ProductGetDto? Product { get; set; }
        public CenterGetDto? Center { get; set; }
    }
}
