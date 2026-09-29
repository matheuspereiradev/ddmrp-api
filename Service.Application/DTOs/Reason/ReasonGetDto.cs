using Service.Application.DTOs.ReasonGroup;

namespace Service.Application.DTOs.Reason
{
    public class ReasonGetDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public bool IsFromSystem { get; set; }
        public int IdReasonGroup { get; set; }
        public ReasonGroupGetDto? ReasonGroup { get; set; }
    }
}
