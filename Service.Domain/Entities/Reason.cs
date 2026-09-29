namespace Service.Domain.Entities
{
    public class Reason : BaseEntity
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public bool IsFromSystem { get; set; }
        public int IdReasonGroup { get; set; }
        public ReasonGroup? ReasonGroup { get; set; }
    }
}
