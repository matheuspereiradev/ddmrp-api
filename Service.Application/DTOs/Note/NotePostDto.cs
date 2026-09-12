using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.Note
{
    public class NotePostDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        [MaxLength(1000, ErrorMessage = "The field {0} has the maxlength 1000")]
        public string Content { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public int CenterProductId { get; set; }
    }
}
