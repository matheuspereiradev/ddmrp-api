namespace Service.Domain.Report.Results
{
    public class QualifiedDemandDayRow
    {
        public DateTime Date { get; set; }
        public decimal TotalPendingQuantity { get; set; }
        public decimal ConfiguredThreshold { get; set; }
        public decimal? ThresholdValue { get; set; }
        public bool IsQualified { get; set; }
        public decimal QualifiedQuantity { get; set; }

        public List<QualifiedDemandOrderRow> Orders { get; set; } = new();
    }
}
