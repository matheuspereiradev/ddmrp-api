using Service.API.Filters;
using Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RobotController : Controller
    {
        private readonly IRobotService _robotService;

        public RobotController(IRobotService robotService)
        {
            _robotService = robotService;
        }

        [HttpPost("run")]
        [Authorize]
        [RequirePermission]
        public async Task<ActionResult> Run(CancellationToken cancellationToken)
        {
            var result = await _robotService.RunAsync(cancellationToken);
            return Ok(result);
        }
    }
}
