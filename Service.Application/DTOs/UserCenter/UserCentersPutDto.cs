using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.UserCenter
{
    public class UserCentersPutDto
    {
        [Required]
        public List<int> CenterIds { get; set; } = new();
    }
}
