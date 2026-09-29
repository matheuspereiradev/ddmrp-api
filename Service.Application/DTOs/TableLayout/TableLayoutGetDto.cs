namespace Service.Application.DTOs.TableLayout
{
    public class TableLayoutGetDto
    {
        public string TableName { get; set; }
        public List<TableLayoutColumnDto> Columns { get; set; } = new();
        public DateTime UpdatedAt { get; set; }
    }
}
