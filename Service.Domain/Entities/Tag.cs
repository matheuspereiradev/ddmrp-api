namespace Service.Domain.Entities
{
    public class Tag : BaseEntity
    {
        public string Name { get; set; }
        public string? Description { get; set; }
    }
}
