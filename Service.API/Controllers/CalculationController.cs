using Service.API.Filters;
using Service.Application.DTOs.Calculation;
using Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CalculationController : Controller
    {
        private readonly ICalculationService _calculationService;

        public CalculationController(ICalculationService calculationService)
        {
            _calculationService = calculationService;
        }

        [HttpPost("run")]
        [Authorize]
        [RequirePermission]
        public async Task<ActionResult> Run(CalculationRunRequestDto? request, CancellationToken cancellationToken)
        {
            var results = await _calculationService.RunAsync(request?.IdCenterProduct, cancellationToken);
            return Ok(results);
        }
    }
}
