using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.User
{
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        [MaxLength(100, ErrorMessage = "The field {0} has the maxlength 100")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [MaxLength(100, ErrorMessage = "The field {0} has the maxlength 100")]
        public string NewPassword { get; set; }
    }
}
