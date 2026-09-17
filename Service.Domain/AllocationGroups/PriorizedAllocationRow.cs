namespace Service.Domain.AllocationGroups
{
    public class PriorizedAllocationRow
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal ApprovedQuantityUnit { get; set; }
        public decimal? ApprovedQuantityWeight { get; set; }
        public decimal? ApprovedQuantityVolume { get; set; }
        public decimal? ApprovedQuantityValue { get; set; }
        public decimal? ApprovedQuantityPallet { get; set; }
    }
}
