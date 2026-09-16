namespace Service.Domain.Report.Results
{
    public class InventoryHistoryRow
    {
        public DateTime Date { get; set; }
        public decimal? Stock { get; set; }
        public decimal? QualifiedDemand { get; set; }
        public decimal? OrdersInTransit { get; set; }
        public decimal? Consumption { get; set; }
        public decimal? StockTotal { get; set; }
        public decimal? Adu { get; set; }
        public decimal? RedSafeZone { get; set; }
        public decimal? RedBaseZone { get; set; }
        public decimal? RedZone { get; set; }
        public decimal? YellowZone { get; set; }
        public decimal? GreenZone { get; set; }
        public decimal? InventoryDays { get; set; }
        public decimal Netflow { get; set; }
    }
}
