using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.Ai
{
    public class AskRequestDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        public string Question { get; set; }
    }
}
