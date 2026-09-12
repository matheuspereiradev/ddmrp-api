namespace Service.Domain.Entities
{
    public class Center : BaseEntity
    {
        public string Code { get; set; }
        public string Description { get; set; }
        public string? City { get; set; }
        public string? Zone { get; set; }
    }
}
