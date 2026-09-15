using System.ComponentModel.DataAnnotations;
using Service.Domain.Enums;

namespace Service.Application.DTOs.History
{
    public class SetDiscardStatusDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        [EnumDataType(typeof(DiscardStatus), ErrorMessage = "Field {0} has an invalid value.")]
        public DiscardStatus DiscardStatus { get; set; }
    }
}
