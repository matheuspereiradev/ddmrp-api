using System;
using Service.Application.DTOs.BufferProfile;
using Service.Application.DTOs.Center;
using Service.Application.DTOs.Product;
using Service.Domain.Enums;

namespace Service.Application.DTOs.History
{
    public class HistoryGetDto
    {
        public int Id { get; set; }
        public int IdProduct { get; set; }
        public int IdCenter { get; set; }
        public decimal Consumption { get; set; }
        public DateTime Date { get; set; }
        public DiscardStatus DiscardStatus { get; set; }
        public decimal? Stock { get; set; }
        public decimal? QualifiedDemand { get; set; }
        public decimal? OpenInbounds { get; set; }
        public decimal? OpenOutbound { get; set; }
        public decimal? Adu { get; set; }
        public decimal? RedSafeZone { get; set; }
        public decimal? RedBaseZone { get; set; }
        public decimal? YellowZone { get; set; }
        public decimal? GreenZone { get; set; }
        public decimal? PackQuantity { get; set; }
        public decimal? Moq { get; set; }
        public int? LeadTime { get; set; }
        public int? Frequency { get; set; }
        public int? IdTag { get; set; }
        public int? IdReason { get; set; }
        public int? IdBufferProfile { get; set; }
        public BufferProfileGetDto? BufferProfile { get; set; }
        public decimal? StandardDeviation { get; set; }
        public decimal? Cv { get; set; }
        public int? FutureAduDays { get; set; }
        public int? HistoryAduDays { get; set; }
        public decimal? Adi { get; set; }
        public decimal? ZafRedZone { get; set; }
        public decimal? ZafYellowZone { get; set; }
        public decimal? ZafGreenZone { get; set; }
        public decimal? StockDays { get; set; }
        public decimal? StockTotal { get; set; }
        public ProductGetDto? Product { get; set; }
        public CenterGetDto? Center { get; set; }
    }
}
