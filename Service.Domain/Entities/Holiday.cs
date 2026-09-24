namespace Service.Domain.Entities
{
    public class Holiday : BaseEntity
    {
        public string Name { get; set; }
        public DateTime Date { get; set; }
    }
}
