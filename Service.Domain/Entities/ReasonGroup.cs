namespace Service.Domain.Entities
{
    public class ReasonGroup : BaseEntity
    {
        public string Name { get; set; }
        public bool IsFromSystem { get; set; }
    }
}
