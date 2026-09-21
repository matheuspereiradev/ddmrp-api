using Service.Domain.Ai;

namespace Service.Domain.Interfaces
{
    public interface IAiConnectorClient
    {
        Task<string?> AskAsync(string workspaceId, string threadId, string message, CancellationToken cancellationToken = default);
        Task<List<AiChatMessage>> GetChatHistoryAsync(string workspaceId, string threadId, CancellationToken cancellationToken = default);
    }
}
