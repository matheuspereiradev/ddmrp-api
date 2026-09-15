using Service.API.Extensions;
using Service.API.Models;
using Service.Application.DTOs.History;
using Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HistoryController : Controller
    {
        private readonly IHistoryService _historyService;

        public HistoryController(IHistoryService historyService)
        {
            _historyService = historyService;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult> CreateHistory(HistoryPostDto historyPostDto, CancellationToken cancellationToken)
        {
            var history = await _historyService.AddAsync(historyPostDto, cancellationToken);
            return Ok(history);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> GetAllHistories(
            [FromQuery] int? idProduct,
            [FromQuery] int? idCenter,
            [FromQuery] DateTime? dateStart,
            [FromQuery] DateTime? dateEnd,
            [FromQuery] PaginationParams paginationParams,
            CancellationToken cancellationToken)
        {
            var histories = await _historyService.GetFilteredAsync(idProduct, idCenter, dateStart, dateEnd, paginationParams.PageNumber, paginationParams.PageSize, cancellationToken);

            Response.AddPaginationHeader(
                new PaginationHeader(paginationParams.PageNumber, paginationParams.PageSize, histories.TotalCount, histories.TotalPages));

            return Ok(histories);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult> UpdateHistory(int id, HistoryPutDto historyPutDto, CancellationToken cancellationToken)
        {
            var history = await _historyService.UpdateAsync(id, historyPutDto, cancellationToken);
            return Ok(history);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteHistory(int id, CancellationToken cancellationToken)
        {
            var history = await _historyService.DeleteAsync(id, cancellationToken);
            return Ok(history);
        }

        [HttpPatch("{id}/discard-status")]
        [Authorize]
        public async Task<ActionResult> SetDiscardStatus(int id, SetDiscardStatusDto setDiscardStatusDto, CancellationToken cancellationToken)
        {
            var history = await _historyService.SetDiscardStatusAsync(id, setDiscardStatusDto.DiscardStatus, cancellationToken);
            return Ok(history);
        }
    }
}
