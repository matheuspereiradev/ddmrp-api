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
        public decimal ReservedStock { get; set; }
        public decimal AvailableStock => Stock - ReservedStock;
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
        public bool FixedBufferProfile { get; set; } = false;
        public decimal? RedZoneBase { get; set; }
        public decimal? RedZoneSafe { get; set; }
        public decimal? RedZone => RedZoneBase.HasValue && RedZoneSafe.HasValue ? Math.Ceiling(RedZoneBase.Value + RedZoneSafe.Value) : (decimal?)null;
        public decimal? YellowZone { get; set; }
        public decimal? GreenZone { get; set; }
        public decimal? TopOfRed => RedZoneBase.HasValue && RedZoneSafe.HasValue ? Math.Ceiling(RedZoneBase.Value + RedZoneSafe.Value) : (decimal?)null;
        public decimal? TopOfYellow => RedZoneBase.HasValue && RedZoneSafe.HasValue && YellowZone.HasValue ? Math.Ceiling(RedZoneBase.Value + RedZoneSafe.Value + YellowZone.Value) : (decimal?)null;
        public decimal? TopOfGreen => RedZoneBase.HasValue && RedZoneSafe.HasValue && YellowZone.HasValue && GreenZone.HasValue ? Math.Ceiling(RedZoneBase.Value + RedZoneSafe.Value + YellowZone.Value + GreenZone.Value) : (decimal?)null;
        public decimal? RedZoneExecution => TopOfRed.HasValue ? Math.Ceiling(TopOfRed.Value / 2) : (decimal?)null;
        public decimal? YellowZoneExecution => TopOfRed.HasValue ? Math.Ceiling(TopOfRed.Value / 2) : (decimal?)null;
        public decimal? GreenZoneExecution => YellowZone.HasValue ? Math.Ceiling(YellowZone.Value) : (decimal?)null;
        public decimal? TopOfRedExecution => RedZoneExecution.HasValue ? Math.Ceiling(RedZoneExecution.Value) : (decimal?)null;
        public decimal? TopOfYellowExecution => RedZoneExecution.HasValue && YellowZoneExecution.HasValue ? Math.Ceiling(RedZoneExecution.Value + YellowZoneExecution.Value) : (decimal?)null;
        public decimal? TopOfGreenExecution => RedZoneExecution.HasValue && YellowZoneExecution.HasValue && GreenZoneExecution.HasValue ? Math.Ceiling(RedZoneExecution.Value + YellowZoneExecution.Value + GreenZoneExecution.Value) : (decimal?)null;
        public decimal? RedSafeAnalytical => RedZone.HasValue ? Math.Ceiling(RedZone.Value / 2) : (decimal?)null;
        public decimal? YellowSafeAnalytical => RedZone.HasValue ? Math.Ceiling(RedZone.Value) : (decimal?)null;
        public decimal? GreenAnalytical => GreenZone.HasValue ? Math.Ceiling(GreenZone.Value) : (decimal?)null;
        public decimal? YellowExcessAnalytical => RedZone.HasValue && YellowZone.HasValue && GreenZone.HasValue
            ? ((RedZone.Value + GreenZone.Value) >= (RedZone.Value + YellowZone.Value)
                ? 0
                : Math.Ceiling((RedZone.Value + YellowZone.Value) - (RedZone.Value + GreenZone.Value)))
            : (decimal?)null;
        public decimal? RedExcessAnalytical => TopOfGreen.HasValue && RedZone.HasValue && GreenZone.HasValue && YellowExcessAnalytical.HasValue
            ? (TopOfGreen.Value <= 0
                ? 0
                : Math.Ceiling(TopOfGreen.Value - (RedZone.Value + GreenZone.Value + YellowExcessAnalytical.Value)))
            : (decimal?)null;
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
