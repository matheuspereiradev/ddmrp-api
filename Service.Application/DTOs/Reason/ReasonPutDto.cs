using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.Reason
{
    public class ReasonPutDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        [MaxLength(100, ErrorMessage = "The field {0} has the maxlength 100")]
        public string Name { get; set; }

        [MaxLength(300, ErrorMessage = "The field {0} has the maxlength 300")]
        public string? Description { get; set; }
    }
}
