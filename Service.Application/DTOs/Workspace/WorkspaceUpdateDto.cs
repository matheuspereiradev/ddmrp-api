using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.Workspace
{
    public class WorkspaceUpdateDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        public int IdCenter { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public int IdProduct { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public decimal OptimizedQuantity { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        public bool Approved { get; set; }
    }
}
