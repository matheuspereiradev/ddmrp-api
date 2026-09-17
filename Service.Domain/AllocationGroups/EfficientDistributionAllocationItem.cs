using Service.Domain.Enums;

namespace Service.Domain.AllocationGroups
{
    public class EfficientDistributionAllocationItem
    {
        public int Id { get; set; }
        public int IdCenter { get; set; }
        public int IdProduct { get; set; }
        public decimal ApprovedQuantity { get; set; }
        public decimal Netflow { get; set; }
        public decimal Tog { get; set; }
        public decimal Moq { get; set; }
        public decimal PackQuantity { get; set; }
        public decimal? ProductWeight { get; set; }
        public decimal? ProductVolume { get; set; }
        public decimal? ProductValue { get; set; }
        public decimal? ProductPallet { get; set; }
        public bool Finished { get; set; }
        public decimal Percentage => Tog != 0 ? (Netflow + ApprovedQuantity) / Tog : 0;

        // The engine always steps/persists ApprovedQuantity in raw units - this only converts the *comparison*
        // against the group limit into the requested unit of measure. Pallet is expressed as a division
        // (quantity / Product.Pallet), normalized here into a multiplier so every adjustment type shares the
        // same ApprovedQuantity * factor formula. Returns null when the product is missing the property the
        // requested AdjustmentType needs (or Pallet is 0), meaning this item can't be valorized at all.
        public decimal? GetValorizationFactor(EfficientDistributionAdjustmentType adjustmentType) => adjustmentType switch
        {
            EfficientDistributionAdjustmentType.Unit => 1,
            EfficientDistributionAdjustmentType.Weight => ProductWeight,
            EfficientDistributionAdjustmentType.Volume => ProductVolume,
            EfficientDistributionAdjustmentType.Value => ProductValue,
            EfficientDistributionAdjustmentType.Pallet => ProductPallet is > 0 ? 1 / ProductPallet : null,
            _ => 1
        };

        public decimal? GetValorizedApprovedQuantity(EfficientDistributionAdjustmentType adjustmentType) =>
            GetValorizationFactor(adjustmentType) is { } factor ? ApprovedQuantity * factor : null;

        public decimal? GetValorizedPackQuantity(EfficientDistributionAdjustmentType adjustmentType) =>
            GetValorizationFactor(adjustmentType) is { } factor ? PackQuantity * factor : null;
    }
}
