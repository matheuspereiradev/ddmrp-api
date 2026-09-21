using Service.Application.DTOs.Ai;
using Service.Domain.Ai;

namespace Service.Application.Interfaces
{
    public interface IAiService
    {
        Task<AskResponseDto> AskAsync(string question, CancellationToken cancellationToken = default);
        Task<List<AiChatMessage>> GetChatHistoryAsync(CancellationToken cancellationToken = default);
    }
}
