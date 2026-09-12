using Service.Application.DTOs.AllocationGroup;
using Service.Application.DTOs.Center;
using Service.Application.DTOs.Partner;
using Service.Application.DTOs.Product;
using Service.Application.DTOs.Reason;
using Service.Application.DTOs.Tag;

namespace Service.Application.DTOs.CenterProduct
{
    public class CenterProductGetDto
    {
        public int Id { get; set; }
        public int IdProduct { get; set; }
        public int IdCenter { get; set; }
        public int? IdOriginCenter { get; set; }
        public decimal PackQuantity { get; set; }
        public decimal Moq { get; set; }
        public int LeadTime { get; set; }
        public int Frequency { get; set; }
        public string? Class { get; set; }
        public string? Classification { get; set; }
        public string? Segment { get; set; }
        public decimal Stock { get; set; }
        public int? IdProvider { get; set; }
        public int? IdTag { get; set; }
        public int? IdReason { get; set; }
        public int? IdAllocationGroup { get; set; }
        public ProductGetDto? Product { get; set; }
        public CenterGetDto? Center { get; set; }
        public CenterGetDto? OriginCenter { get; set; }
        public PartnerGetDto? Provider { get; set; }
        public TagGetDto? Tag { get; set; }
        public ReasonGetDto? Reason { get; set; }
        public AllocationGroupGetDto? AllocationGroup { get; set; }
    }
}
