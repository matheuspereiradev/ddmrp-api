using Service.Application.DTOs.Center;
using Service.Application.DTOs.Product;

namespace Service.Application.DTOs.MasterBuffer
{
    public class MasterBufferGetDto
    {
        public int Id { get; set; }
        public int IdProduct { get; set; }
        public int IdCenter { get; set; }
        public int IdProductFather { get; set; }
        public int IdCenterFather { get; set; }
        public int Sequency { get; set; }
        public ProductGetDto? Product { get; set; }
        public CenterGetDto? Center { get; set; }
        public ProductGetDto? ProductFather { get; set; }
        public CenterGetDto? CenterFather { get; set; }
    }
}
