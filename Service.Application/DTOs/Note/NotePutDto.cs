using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.Note
{
    public class NotePutDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        [MaxLength(1000, ErrorMessage = "The field {0} has the maxlength 1000")]
        public string Content { get; set; }
    }
}
