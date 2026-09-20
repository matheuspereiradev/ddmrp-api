using Service.Application.DTOs.AllocationGroup;
using Service.Application.DTOs.BufferProfile;
using Service.Application.DTOs.Center;
using Service.Application.DTOs.Partner;
using Service.Application.DTOs.Product;
using Service.Application.DTOs.Reason;
using Service.Application.DTOs.Tag;
using Service.Domain.Enums;

namespace Service.Application.DTOs.CenterProduct
{
    public class CenterProductGetDto
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
        public decimal ReservedStock { get; set; }
        public decimal AvailableStock { get; set; }
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
        public bool FixedBufferProfile { get; set; }
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
        public decimal? TopOfRedExecution { get; set; }
        public decimal? TopOfYellowExecution { get; set; }
        public decimal? TopOfGreenExecution { get; set; }
        public decimal? RedSafeAnalytical { get; set; }
        public decimal? YellowSafeAnalytical { get; set; }
        public decimal? GreenAnalytical { get; set; }
        public decimal? YellowExcessAnalytical { get; set; }
        public decimal? RedSafeExcessAnalytical { get; set; }
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
        public ProductGetDto? Product { get; set; }
        public CenterGetDto? Center { get; set; }
        public CenterGetDto? OriginCenter { get; set; }
        public PartnerGetDto? Provider { get; set; }
        public TagGetDto? Tag { get; set; }
        public ReasonGetDto? Reason { get; set; }
        public AllocationGroupGetDto? AllocationGroup { get; set; }
        public BufferProfileGetDto? BufferProfile { get; set; }
    }
}
