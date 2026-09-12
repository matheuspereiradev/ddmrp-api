using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.Center
{
    public class CenterPutDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        [MaxLength(20, ErrorMessage = "The field {0} has the maxlength 20")]
        public string Code { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [MaxLength(200, ErrorMessage = "The field {0} has the maxlength 200")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [MaxLength(100, ErrorMessage = "The field {0} has the maxlength 100")]
        public string City { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [MaxLength(100, ErrorMessage = "The field {0} has the maxlength 100")]
        public string Zone { get; set; }
    }
}
