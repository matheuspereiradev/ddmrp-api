using System.ComponentModel.DataAnnotations;

namespace Service.API.Models
{
    public class UserLogin
    {
        [Required(ErrorMessage = "Email is required.")]
        [MaxLength(250, ErrorMessage = "Email must be at most 250 characters long.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
        [MaxLength(250, ErrorMessage = "Password must be at most 250 characters long.")]
        public string Password { get; set; }
    }
}
