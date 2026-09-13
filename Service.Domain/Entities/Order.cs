using Service.Domain.Enums;

namespace Service.Domain.Entities
{
    public class Order : BaseEntity
    {
        public string OrderNumber { get; set; }
        public int? IdPartner { get; set; }
        public Partner? Partner { get; set; }
        public int? IdDestinyCenter { get; set; }
        public Center? DestinyCenter { get; set; }
        public int? IdOriginCenter { get; set; }
        public Center? OriginCenter { get; set; }
        public int IdProduct { get; set; }
        public Product Product { get; set; }
        public decimal Quantity { get; set; }
        public decimal DeliveredQuantity { get; set; }
        public string MeasurementUnit { get; set; }
        public int? Position { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? Notes { get; set; }
        public OrderType Type { get; set; }
        public bool IsInbound { get; set; }
        public bool IsOutbound { get; set; }
        public bool IsFictional { get; set; } = false;
    }
}
