using Service.Application.DTOs.Center;
using Service.Application.DTOs.Partner;
using Service.Application.DTOs.Product;
using Service.Domain.Enums;

namespace Service.Application.DTOs.Order
{
    public class OrderGetDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public int? IdPartner { get; set; }
        public int? IdDestinyCenter { get; set; }
        public int? IdOriginCenter { get; set; }
        public int IdProduct { get; set; }
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
        public bool IsFictional { get; set; }
        public PartnerGetDto? Partner { get; set; }
        public CenterGetDto? DestinyCenter { get; set; }
        public CenterGetDto? OriginCenter { get; set; }
        public ProductGetDto? Product { get; set; }
    }
}
