namespace Service.Domain.Report.Results
{
    public class BufferColorHistoryDayRow
    {
        public DateTime Date { get; set; }
        public int Red { get; set; }
        public int Yellow { get; set; }
        public int Green { get; set; }
        public int Blue { get; set; }
        public int Black { get; set; }
        public int NoColor { get; set; }
    }
}
