using System;
using Service.Domain.Enums;

namespace Service.Domain.Entities
{
    public class History : BaseEntity
    {
        public int IdProduct { get; set; }
        public Product Product { get; set; }
        public int IdCenter { get; set; }
        public Center Center { get; set; }
        public decimal Consumption { get; set; }
        public DateTime Date { get; set; }
        public DiscardStatus DiscardStatus { get; set; } = DiscardStatus.NotReviewed;
        public decimal? Stock { get; set; }
        public decimal? ReservedStock { get; set; }
        public decimal? AvailableStock => Stock.HasValue && ReservedStock.HasValue ? Stock.Value - ReservedStock.Value : null;
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
        public BufferProfile? BufferProfile { get; set; }
        public decimal? StandardDeviation { get; set; }
        public decimal? Cv { get; set; }
        public int? FutureAduDays { get; set; }
        public int? HistoryAduDays { get; set; }
        public decimal? Adi { get; set; }
        public decimal? ZafRedZone { get; set; }
        public decimal? ZafYellowZone { get; set; }
        public decimal? ZafGreenZone { get; set; }
        public decimal? StockDays => Stock.HasValue && Adu.HasValue && Adu.Value != 0 ? Stock.Value / Adu.Value : null;
        public decimal? StockTotal => Stock.HasValue && OpenInbounds.HasValue ? Stock.Value + OpenInbounds.Value : null;
    }
}
