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
        public decimal Quantity { get; set; }
        public DateTime Date { get; set; }
        public DiscardStatus DiscardStatus { get; set; } = DiscardStatus.NotReviewed;
    }
}
