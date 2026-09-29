using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Application.DTOs.TableLayout;
using Service.Application.Interfaces;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TableLayoutController : Controller
    {
        private readonly ITableLayoutService _tableLayoutService;

        public TableLayoutController(ITableLayoutService tableLayoutService)
        {
            _tableLayoutService = tableLayoutService;
        }

        [HttpGet("{tableName}")]
        [Authorize]
        public async Task<ActionResult> GetTableLayout(string tableName, CancellationToken cancellationToken)
        {
            var tableLayout = await _tableLayoutService.GetAsync(tableName, cancellationToken);
            return Ok(tableLayout);
        }

        [HttpPut("{tableName}")]
        [Authorize]
        public async Task<ActionResult> SaveTableLayout(string tableName, TableLayoutPutDto putDto, CancellationToken cancellationToken)
        {
            var tableLayout = await _tableLayoutService.SaveAsync(tableName, putDto, cancellationToken);
            return Ok(tableLayout);
        }

        [HttpDelete("{tableName}")]
        [Authorize]
        public async Task<ActionResult> DeleteTableLayout(string tableName, CancellationToken cancellationToken)
        {
            await _tableLayoutService.DeleteAsync(tableName, cancellationToken);
            return Ok();
        }
    }
}
