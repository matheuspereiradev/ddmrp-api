using Service.Domain.Enums;

namespace Service.Domain.Report.Results
{
    public class OpenOrderRow
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;

        public int? IdPartner { get; set; }
        public string? PartnerCode { get; set; }
        public string? PartnerDescription { get; set; }

        public int? IdDestinyCenter { get; set; }
        public string? DestinyCenterCode { get; set; }
        public string? DestinyCenterDescription { get; set; }

        public int? IdOriginCenter { get; set; }
        public string? OriginCenterCode { get; set; }
        public string? OriginCenterDescription { get; set; }

        public int IdProduct { get; set; }
        public string ProductReference { get; set; } = string.Empty;
        public string ProductDescription { get; set; } = string.Empty;

        public decimal Quantity { get; set; }
        public decimal DeliveredQuantity { get; set; }
        public decimal PendingQuantity { get; set; }
        public string MeasurementUnit { get; set; } = string.Empty;
        public int? Position { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public int? OrderLeadtime { get; set; }
        public int DaysToReceive { get; set; }
        public int DaysLate { get; set; }
        public decimal? TimeBuffer { get; set; }
        public BufferColor? TimeBufferColor { get; set; }
        public decimal? ExecutionBuffer { get; set; }
        public BufferColor? ExecutionBufferColor { get; set; }
        public string? Notes { get; set; }
        public OrderType Type { get; set; }
        public bool IsInbound { get; set; }
        public bool IsOutbound { get; set; }
        public bool IsFictional { get; set; }
    }
}
