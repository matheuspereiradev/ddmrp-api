namespace Service.Domain.Entities
{
    public class CenterProduct : BaseEntity
    {
        public int IdProduct { get; set; }
        public Product Product { get; set; }
        public int IdCenter { get; set; }
        public Center Center { get; set; }
        public int? IdOriginCenter { get; set; }
        public Center? OriginCenter { get; set; }
        public decimal PackQuantity { get; set; }
        public decimal Moq { get; set; }
        public int LeadTime { get; set; }
        public int Frequency { get; set; }
        public string? Class { get; set; }
        public string? Classification { get; set; }
        public string? Segment { get; set; }
        public decimal Stock { get; set; }
        public int? IdProvider { get; set; }
        public Partner? Provider { get; set; }
        public int? IdTag { get; set; }
        public Tag? Tag { get; set; }
        public int? IdReason { get; set; }
        public Reason? Reason { get; set; }
        public int? IdAllocationGroup { get; set; }
        public AllocationGroup? AllocationGroup { get; set; }
        public int? IdBufferProfile { get; set; }
        public BufferProfile? BufferProfile { get; set; }
        public decimal? Adu { get; set; }
        public int? FutureAduDays { get; set; }
        public int? HistoryAduDays { get; set; }
    }
}
