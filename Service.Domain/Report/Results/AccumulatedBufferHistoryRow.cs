namespace Service.Domain.Report.Results
{
    public class AccumulatedBufferHistoryRow
    {
        public DateTime Date { get; set; }
        public decimal ExecutionRedZone { get; set; }
        public decimal ExecutionYellowZone { get; set; }
        public decimal ExecutionGreenZone { get; set; }
        public decimal NetflowRedZone { get; set; }
        public decimal NetflowYellowZone { get; set; }
        public decimal NetflowGreenZone { get; set; }
        public decimal RedSafeAnalytical { get; set; }
        public decimal YellowSafeAnalytical { get; set; }
        public decimal GreenAnalytical { get; set; }
        public decimal YellowExcessAnalytical { get; set; }
        public decimal RedExcessAnalytical { get; set; }
        public decimal AverageProjectedInventory { get; set; }
        public decimal AvailableStock { get; set; }
        public decimal Netflow { get; set; }
        public decimal ExcessStock { get; set; }
        public decimal ExcessStockAnalytical { get; set; }
        public decimal MinimumOscillationRange { get; set; }
        public decimal MaximumOscillationRange { get; set; }
    }
}
