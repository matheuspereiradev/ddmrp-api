using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.Tag
{
    public class TagPutDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        [MaxLength(100, ErrorMessage = "The field {0} has the maxlength 100")]
        public string Name { get; set; }

        [MaxLength(250, ErrorMessage = "The field {0} has the maxlength 250")]
        public string? Description { get; set; }
    }
}
