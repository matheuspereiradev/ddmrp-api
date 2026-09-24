using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.Holiday
{
    public class HolidayPostDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        [MaxLength(100, ErrorMessage = "The field {0} has the maxlength 100")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public DateTime Date { get; set; }

        public bool IsRecurring { get; set; }
    }
}
