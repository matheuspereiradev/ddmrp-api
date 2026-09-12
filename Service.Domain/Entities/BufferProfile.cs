using Service.Domain.Enums;

namespace Service.Domain.Entities
{
    public class BufferProfile : BaseEntity
    {
        public string ProfileName { get; set; }
        public SupplyType SupplyType { get; set; }
        public LeadTimeCategory LeadTimeCategory { get; set; }
        public VariabilityCategory VariabilityCategory { get; set; }
        public decimal LeadTimeFactor { get; set; }
        public decimal VariabilityFactor { get; set; }
        public int AduCalculationDays { get; set; }
        public int AduFutureDays { get; set; }
        public int Frequency { get; set; }
        public bool UseAdUxDlTxFactorDlt { get; set; }
        public bool UseMoq { get; set; }
        public bool UseAdUxFrequency { get; set; }
        public SpikeHorizonType SpikeHorizonType { get; set; }
        public int SpikeHorizonValue { get; set; }
        public int SpikeHorizonLTDays { get; set; }
        public SpikeThresholdType SpikeThresholdType { get; set; }
        public int SpikeThresholdAdu { get; set; }
        public decimal SpikeThresholdPercentageRedZone { get; set; }
        public bool IsActive { get; set; }
        public bool IsMakeToOrder { get; set; }
    }
}
