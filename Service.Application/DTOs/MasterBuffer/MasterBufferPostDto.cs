using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.MasterBuffer
{
    public class MasterBufferPostDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        public int IdProduct { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public int IdCenter { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public int IdProductFather { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public int IdCenterFather { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public int Sequency { get; set; }
    }
}
