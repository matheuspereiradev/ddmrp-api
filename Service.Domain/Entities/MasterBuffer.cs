namespace Service.Domain.Entities
{
    public class MasterBuffer : BaseEntity
    {
        public int IdProduct { get; set; }
        public Product Product { get; set; }
        public int IdCenter { get; set; }
        public Center Center { get; set; }
        public int IdProductFather { get; set; }
        public Product ProductFather { get; set; }
        public int IdCenterFather { get; set; }
        public Center CenterFather { get; set; }
        public int Sequency { get; set; }
    }
}
