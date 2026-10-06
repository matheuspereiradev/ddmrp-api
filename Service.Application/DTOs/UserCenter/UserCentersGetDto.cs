using Service.Application.DTOs.Center;

namespace Service.Application.DTOs.UserCenter
{
    public class UserCentersGetDto
    {
        public int IdUser { get; set; }
        public bool HasAllCentersAccess { get; set; }
        public List<CenterGetDto> Centers { get; set; } = new();
    }
}
