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
        public async Task<ActionResult> Run(CancellationToken cancellationToken)
        {
            var results = await _calculationService.RunAsync(cancellationToken);
            return Ok(results);
        }
    }
}
