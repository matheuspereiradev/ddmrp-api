namespace Service.Application.DTOs.UserCenter
{
    public class MyCentersGetDto
    {
        public int IdUser { get; set; }
        public bool HasAllCentersAccess { get; set; }
        public List<int> Centers { get; set; } = new();
    }
}
