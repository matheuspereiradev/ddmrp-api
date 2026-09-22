using System.ComponentModel.DataAnnotations;

namespace Service.Application.DTOs.ForecastAI
{
    public class ApplyForecastAIDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        public int Id { get; set; }
    }
}
