using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.TableLayout
{
    public class TableLayoutPutDto
    {
        [Required]
        [MaxLength(200)]
        public List<TableLayoutColumnDto> Columns { get; set; } = new();
    }
}
