namespace Service.Domain.Entities
{
    public class UserCenter
    {
        public int Id { get; set; }
        public int IdUser { get; set; }
        public User User { get; set; }
        public int IdCenter { get; set; }
        public Center Center { get; set; }
    }
}
