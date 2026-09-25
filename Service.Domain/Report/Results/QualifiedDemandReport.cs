using Service.Domain.Enums;

namespace Service.Domain.Report.Results
{
    public class QualifiedDemandReport
    {
        public int IdCenterProduct { get; set; }
        public int IdProduct { get; set; }
        public int IdCenter { get; set; }

        public int LeadTime { get; set; }
        public decimal? Adu { get; set; }
        public decimal? RedZoneBase { get; set; }
        public decimal? RedZoneSafe { get; set; }

        public SpikeThresholdType SpikeThresholdType { get; set; }
        public decimal SpikeThresholdAdu { get; set; }
        public decimal SpikeThresholdPercentageRedZone { get; set; }

        // The resolved real-quantity threshold (limiar) every day in this report is compared against —
        // Adu * SpikeThresholdAdu when SpikeThresholdType = Adu, or (RedZoneBase + RedZoneSafe) *
        // SpikeThresholdPercentageRedZone when SpikeThresholdType = PlanningRedZone. Null only when
        // SpikeThresholdType = Adu and Adu itself hasn't been calculated yet for this CenterProduct.
        public decimal? UsedSpikeThreshold { get; set; }

        public SpikeHorizonType SpikeHorizonType { get; set; }
        public int SpikeHorizonValue { get; set; }
        public int SpikeHorizonLTDays { get; set; }
        public int HorizonDays { get; set; }
        public DateTime HorizonStartDate { get; set; }
        public DateTime HorizonEndDate { get; set; }

        public decimal TotalQualifiedDemand { get; set; }

        public List<QualifiedDemandDayRow> Days { get; set; } = new();
    }
}
