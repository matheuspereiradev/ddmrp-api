namespace Service.Domain.Entities
{
    public class TableLayoutColumn
    {
        public string ColumnId { get; set; }
        public int Order { get; set; }
        public bool Visible { get; set; }
        public bool IsFixed { get; set; }
        public string? Label { get; set; }
        public string? Color { get; set; }
    }
}
