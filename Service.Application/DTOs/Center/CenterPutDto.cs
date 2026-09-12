using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.Center
{
    public class CenterPutDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        [MaxLength(50, ErrorMessage = "The field {0} has the maxlength 50")]
        public string Code { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [MaxLength(200, ErrorMessage = "The field {0} has the maxlength 200")]
        public string Description { get; set; }

        [MaxLength(100, ErrorMessage = "The field {0} has the maxlength 100")]
        public string? City { get; set; }

        [MaxLength(100, ErrorMessage = "The field {0} has the maxlength 100")]
        public string? Zone { get; set; }
    }
}
