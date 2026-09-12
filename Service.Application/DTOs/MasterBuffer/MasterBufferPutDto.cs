using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.MasterBuffer
{
    public class MasterBufferPutDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        public int Sequency { get; set; }
    }
}
