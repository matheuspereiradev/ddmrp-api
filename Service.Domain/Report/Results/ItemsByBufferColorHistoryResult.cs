namespace Service.Domain.Report.Results
{
    public class ItemsByBufferColorHistoryResult
    {
        public List<BufferColorHistoryDayRow> Execution { get; set; } = new();
        public List<BufferColorHistoryDayRow> Netflow { get; set; } = new();
    }
}
