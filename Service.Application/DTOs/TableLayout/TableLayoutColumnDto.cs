using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.TableLayout
{
    public class TableLayoutColumnDto
    {
        [Required]
        [MaxLength(100)]
        public string ColumnId { get; set; }

        [Range(0, int.MaxValue)]
        public int Order { get; set; }

        public bool Visible { get; set; }

        public bool IsFixed { get; set; }

        public string? Label { get; set; }

        [RegularExpression("^#[0-9A-Fa-f]{6}$")]
        public string? Color { get; set; }
    }
}
