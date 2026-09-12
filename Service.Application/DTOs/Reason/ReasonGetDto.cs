namespace Service.Application.DTOs.Reason
{
    public class ReasonGetDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public bool IsFromSystem { get; set; }
    }
}
