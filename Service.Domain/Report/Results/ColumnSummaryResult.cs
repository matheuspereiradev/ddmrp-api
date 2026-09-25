namespace Service.Domain.Report.Results
{
    public class ColumnSummaryResult
    {
        public decimal? Sum { get; set; }
        public decimal? Avg { get; set; }
        public decimal? Max { get; set; }
        public decimal? Min { get; set; }
    }
}
