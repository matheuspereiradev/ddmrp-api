using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.UserCenter
{
    public class SetAllCentersAccessDto
    {
        [Required]
        public bool? HasAllCentersAccess { get; set; }
    }
}
