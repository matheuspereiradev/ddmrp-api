namespace Service.Domain.Entities
{
    public class Workspace : BaseEntity
    {
        public int IdCenter { get; set; }
        public Center Center { get; set; }
        public int IdProduct { get; set; }
        public Product Product { get; set; }
        public int IdUser { get; set; }
        public User User { get; set; }
        public decimal OptimizedQuantity { get; set; }
        public bool Approved { get; set; }
    }
}
