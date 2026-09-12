namespace Service.Domain.Entities
{
    public class Note : BaseEntity
    {
        public string Content { get; set; }
        public int CenterProductId { get; set; }
        public CenterProduct CenterProduct { get; set; }
        public User? CreatedByUser { get; set; }
    }
}
