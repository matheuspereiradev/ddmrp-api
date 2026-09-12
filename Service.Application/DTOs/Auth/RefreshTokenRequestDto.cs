using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.Auth
{
    public class RefreshTokenRequestDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        public string RefreshToken { get; set; }
    }
}
