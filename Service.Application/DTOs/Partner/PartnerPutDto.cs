using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.Partner
{
    public class PartnerPutDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        [MaxLength(50, ErrorMessage = "The field {0} has the maxlength 50")]
        public string Code { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [MaxLength(200, ErrorMessage = "The field {0} has the maxlength 200")]
        public string Description { get; set; }
    }
}
