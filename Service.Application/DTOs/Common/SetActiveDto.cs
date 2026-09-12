using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.Common
{
    public class SetActiveDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        public bool IsActive { get; set; }
    }
}
