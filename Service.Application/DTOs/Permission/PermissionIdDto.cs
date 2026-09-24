using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.Permission
{
    public class PermissionIdDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        public string PermissionId { get; set; }
    }
}
