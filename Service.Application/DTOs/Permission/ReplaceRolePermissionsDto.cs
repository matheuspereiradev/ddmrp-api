using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.Permission
{
    public class ReplaceRolePermissionsDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        public List<string> PermissionIds { get; set; } = new();
    }
}
