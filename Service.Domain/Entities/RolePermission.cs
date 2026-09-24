namespace Service.Domain.Entities
{
    public class RolePermission
    {
        public int IdRole { get; set; }
        public Role Role { get; set; }
        public string IdPermission { get; set; }
        public Permission Permission { get; set; }
    }
}
