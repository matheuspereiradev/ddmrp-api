using Service.Application.DTOs.Ai;
using Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class AiController : Controller
    {
        private readonly IAiService _aiService;

        public AiController(IAiService aiService)
        {
            _aiService = aiService;
        }

        [HttpPost("ask")]
        [Authorize]
        public async Task<ActionResult> Ask(AskRequestDto askRequestDto, CancellationToken cancellationToken)
        {
            var response = await _aiService.AskAsync(askRequestDto.Question, cancellationToken);
            return Ok(response);
        }

        [HttpGet("chat")]
        [Authorize]
        public async Task<ActionResult> GetChatHistory(CancellationToken cancellationToken)
        {
            var history = await _aiService.GetChatHistoryAsync(cancellationToken);
            return Ok(history);
        }
    }
}
