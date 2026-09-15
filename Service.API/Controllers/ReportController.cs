using Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportController : Controller
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("inventoryBufferManagement")]
        [Authorize]
        public async Task<ActionResult> InventoryBufferManagement(CancellationToken cancellationToken)
        {
            var result = await _reportService.GetInventoryBufferManagementAsync(cancellationToken);
            return Ok(result);
        }

        [HttpGet("openOrders/inbounds")]
        [Authorize]
        public async Task<ActionResult> OpenOrders([FromQuery] int? idCenter, [FromQuery] int? idProduct, CancellationToken cancellationToken)
        {
            var result = await _reportService.GetOpenOrdersAsync(idCenter, idProduct, cancellationToken);
            return Ok(result);
        }
    }
}
