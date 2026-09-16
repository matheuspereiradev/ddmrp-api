using Service.Domain.Enums;

namespace Service.Domain.Report.Results
{
    public class ProjectedStockAlertRow
    {
        public DateTime Date { get; set; }
        public int IdProduct { get; set; }
        public int IdCenter { get; set; }
        public string ProductReference { get; set; } = string.Empty;
        public string CenterCode { get; set; } = string.Empty;
        public decimal Adu { get; set; }
        public decimal RedZoneExecution { get; set; }
        public decimal YellowZoneExecution { get; set; }
        public decimal GreenZoneExecution { get; set; }
        public decimal ProjectedConsumption { get; set; }
        public decimal Inbound { get; set; }
        public decimal OutboundOrders { get; set; }
        public decimal Outbound { get; set; }
        public decimal OpeningStock { get; set; }
        public decimal ClosingStock { get; set; }
        public BufferColor ExecutionBufferColor { get; set; }
    }
}
