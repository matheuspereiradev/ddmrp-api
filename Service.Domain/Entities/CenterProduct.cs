using Service.Domain.Enums;

namespace Service.Domain.Entities
{
    public class CenterProduct : BaseEntity
    {
        public int IdProduct { get; set; }
        public Product Product { get; set; }
        public int IdCenter { get; set; }
        public Center Center { get; set; }
        public int? IdOriginCenter { get; set; }
        public Center? OriginCenter { get; set; }
        public decimal PackQuantity { get; set; }
        public decimal Moq { get; set; }
        public int LeadTime { get; set; }
        public int Frequency { get; set; }
        public string? Class { get; set; }
        public string? Classification { get; set; }
        public string? Segment { get; set; }
        public decimal Stock { get; set; }
        public int? IdProvider { get; set; }
        public Partner? Provider { get; set; }
        public int? IdTag { get; set; }
        public Tag? Tag { get; set; }
        public int? IdReason { get; set; }
        public Reason? Reason { get; set; }
        public int? IdAllocationGroup { get; set; }
        public AllocationGroup? AllocationGroup { get; set; }
        public int? IdBufferProfile { get; set; }
        public BufferProfile? BufferProfile { get; set; }
        public decimal? Adu { get; set; }
        public int? FutureAduDays { get; set; }
        public int? HistoryAduDays { get; set; }
        public decimal? Adi { get; set; }
        public decimal? StandardDeviation { get; set; }
        public decimal? Cv { get; set; }
        public bool UseSuggestedLTFactor { get; set; } = true;
        public bool UseSuggestedVariabilityFactor { get; set; } = true;
        public decimal? RedZoneBase { get; set; }
        public decimal? RedZoneSafe { get; set; }
        public decimal? RedZone => RedZoneBase + RedZoneSafe;
        public decimal? YellowZone { get; set; }
        public decimal? GreenZone { get; set; }
        public decimal? TopOfRed => RedZoneBase + RedZoneSafe;
        public decimal? TopOfYellow => RedZoneBase + RedZoneSafe + YellowZone;
        public decimal? TopOfGreen => RedZoneBase + RedZoneSafe + YellowZone + GreenZone;
        public bool UseDafOnGreenZone { get; set; }
        public decimal CustomLeadTimeFactor { get; set; } = 1;
        public decimal CustomVariabilityFactor { get; set; } = 1;
        public bool GreenZoneParametrizationUseMoq { get; set; } = true;
        public bool GreenZoneParametrizationUseAduXFrequency { get; set; } = true;
        public bool GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime { get; set; } = true;
        public BufferType BufferType { get; set; }
        public decimal ZafRedZone { get; set; }
        public decimal ZafYellowZone { get; set; }
        public decimal ZafGreenZone { get; set; }
        public decimal? QualifiedDemand { get; set; }
        public SpikeHorizonType SpikeHorizonType { get; set; }
        public int SpikeHorizonValue { get; set; } = 60;
        public int SpikeHorizonLTDays { get; set; } = 1;
        public SpikeThresholdType SpikeThresholdType { get; set; }
        public decimal SpikeThresholdAdu { get; set; } = 1;
        public decimal SpikeThresholdPercentageRedZone { get; set; } = 0.5m;
    }
}
