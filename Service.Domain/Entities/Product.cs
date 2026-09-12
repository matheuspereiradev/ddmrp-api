namespace Service.Domain.Entities
{
    public class Product : BaseEntity
    {
        public string Reference { get; set; }
        public string Description { get; set; }
        public string? AuxiliarMaterialCode { get; set; }
        public string UnitOfMeasure { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Volume { get; set; }
        public string? Barcode { get; set; }
        public string? Category { get; set; }
        public string? Segment { get; set; }
        public decimal? Value { get; set; }
        public decimal? Pallet { get; set; }
        public string? Line { get; set; }
        public string? Subline { get; set; }
        public string? ABC { get; set; }
        public string? Brand { get; set; }
        public string? WorkCenter { get; set; }
    }
}
