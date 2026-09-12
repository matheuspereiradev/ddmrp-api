using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.AllocationGroup
{
    public class AllocationGroupPostDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        [MaxLength(100, ErrorMessage = "The field {0} has the maxlength 100")]
        public string Name { get; set; }
    }
}
