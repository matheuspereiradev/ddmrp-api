namespace Service.Domain.AllocationGroups
{
    public class EfficientDistributionAllocationItem
    {
        public int Id { get; set; }
        public int IdCenter { get; set; }
        public int IdProduct { get; set; }
        public decimal ApprovedQuantity { get; set; }
        public decimal Netflow { get; set; }
        public decimal Tog { get; set; }
        public decimal Moq { get; set; }
        public decimal PackQuantity { get; set; }
        public bool Finished { get; set; }
        public decimal Percentage => Tog != 0 ? (Netflow + ApprovedQuantity) / Tog : 0;
    }
}
