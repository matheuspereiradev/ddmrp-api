namespace Service.Domain.Report.Results
{
    public class InventoryBufferManagementColorSummaryResult
    {
        public List<BufferColorSummaryRow> Netflow { get; set; } = new();
        public List<BufferColorSummaryRow> Execution { get; set; } = new();
        public List<AnalyticalBufferColorSummaryRow> Analytical { get; set; } = new();
    }
}
