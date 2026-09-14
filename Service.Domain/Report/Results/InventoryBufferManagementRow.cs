using Service.Domain.Enums;

namespace Service.Domain.Report.Results
{
    public class InventoryBufferManagementRow
    {
        public int Id { get; set; }
        public int IdProduct { get; set; }
        public int IdCenter { get; set; }
        public int? IdOriginCenter { get; set; }
        public decimal PackQuantity { get; set; }
        public decimal Moq { get; set; }
        public int LeadTime { get; set; }
        public int Frequency { get; set; }
        public string? Class { get; set; }
        public string? Classification { get; set; }
        public string? Segment { get; set; }
        public decimal Stock { get; set; }
        public int? IdProvider { get; set; }
        public int? IdTag { get; set; }
        public int? IdReason { get; set; }
        public int? IdAllocationGroup { get; set; }
        public int? IdBufferProfile { get; set; }
        public decimal? Adu { get; set; }
        public int? FutureAduDays { get; set; }
        public int? HistoryAduDays { get; set; }
        public decimal? Adi { get; set; }
        public decimal? StandardDeviation { get; set; }
        public decimal? Cv { get; set; }
        public bool UseSuggestedLTFactor { get; set; }
        public bool UseSuggestedVariabilityFactor { get; set; }
        public decimal? RedZoneBase { get; set; }
        public decimal? RedZoneSafe { get; set; }
        public decimal? RedZone { get; set; }
        public decimal? YellowZone { get; set; }
        public decimal? GreenZone { get; set; }
        public decimal? TopOfRed { get; set; }
        public decimal? TopOfYellow { get; set; }
        public decimal? TopOfGreen { get; set; }
        public decimal? RedZoneExecution { get; set; }
        public decimal? YellowZoneExecution { get; set; }
        public decimal? GreenZoneExecution { get; set; }
        public bool UseDafOnGreenZone { get; set; }
        public decimal CustomLeadTimeFactor { get; set; }
        public decimal CustomVariabilityFactor { get; set; }
        public bool GreenZoneParametrizationUseMoq { get; set; }
        public bool GreenZoneParametrizationUseAduXFrequency { get; set; }
        public bool GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime { get; set; }
        public BufferType BufferType { get; set; }
        public decimal ZafRedZone { get; set; }
        public decimal ZafYellowZone { get; set; }
        public decimal ZafGreenZone { get; set; }
        public decimal? QualifiedDemand { get; set; }
        public SpikeHorizonType SpikeHorizonType { get; set; }
        public int SpikeHorizonValue { get; set; }
        public int SpikeHorizonLTDays { get; set; }
        public SpikeThresholdType SpikeThresholdType { get; set; }
        public decimal SpikeThresholdAdu { get; set; }
        public decimal SpikeThresholdPercentageRedZone { get; set; }

        public string CenterCode { get; set; } = string.Empty;
        public string CenterDescription { get; set; } = string.Empty;

        public string ProductReference { get; set; } = string.Empty;
        public string ProductDescription { get; set; } = string.Empty;
        public string? ProductAuxiliarMaterialCode { get; set; }
        public string ProductUnitOfMeasure { get; set; } = string.Empty;
        public decimal? ProductWeight { get; set; }
        public decimal? ProductVolume { get; set; }
        public string? ProductBarcode { get; set; }
        public string? ProductCategory { get; set; }
        public string? ProductSegment { get; set; }
        public decimal? ProductValue { get; set; }
        public decimal? ProductPallet { get; set; }
        public string? ProductLine { get; set; }
        public string? ProductSubline { get; set; }
        public string? ProductBrand { get; set; }
        public string? ProductWorkCenter { get; set; }

        public string? ProviderCode { get; set; }
        public string? ProviderDescription { get; set; }

        public string? BufferProfileName { get; set; }
        public string? TagName { get; set; }
        public string? ReasonName { get; set; }
        public string? AllocationGroupName { get; set; }

        public decimal Inbounds { get; set; }
        public decimal FictionalInbounds { get; set; }
        public decimal Outbounds { get; set; }
        public decimal FictionalOutbounds { get; set; }

        public decimal Netflow { get; set; }
        public decimal OrderQuantity { get; set; }
        public decimal OptimizedOrderQuantity { get; set; }
        public decimal NetflowBufferPercentage { get; set; }
        public BufferColor NetflowBufferColor { get; set; }
        public decimal CoverageDays { get; set; }
        public decimal ExecutionBufferPercentage { get; set; }
        public BufferColor ExecutionBufferColor { get; set; }
    }
}
