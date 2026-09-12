using Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IngestionController : Controller
    {
        private readonly IIngestionService _ingestionService;

        public IngestionController(IIngestionService ingestionService)
        {
            _ingestionService = ingestionService;
        }

        [HttpPost("run")]
        [Authorize]
        public async Task<ActionResult> Run([FromQuery] string? view, CancellationToken cancellationToken)
        {
            var results = await _ingestionService.RunAsync(view, cancellationToken);
            return Ok(results);
        }
    }
}
