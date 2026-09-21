using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Service.Domain.Ai;
using Service.Domain.Interfaces;

namespace Service.Infra.Data.Ai
{
    public class AiConnectorClient : IAiConnectorClient
    {
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _httpClient;
        private readonly string? _apiBearer;

        public AiConnectorClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiBearer = configuration["AiConnector:ApiBearer"];
        }

        public async Task<string?> AskAsync(string workspaceId, string threadId, string message, CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"v1/workspace/{workspaceId}/thread/{threadId}/chat");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiBearer);

            var body = JsonSerializer.Serialize(new { message, mode = "chat" });
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var result = await JsonSerializer.DeserializeAsync<AiConnectorChatResponse>(responseStream, _jsonOptions, cancellationToken);
            return result?.TextResponse;
        }

        public async Task<List<AiChatMessage>> GetChatHistoryAsync(string workspaceId, string threadId, CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"v1/workspace/{workspaceId}/thread/{threadId}/chats");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiBearer);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var result = await JsonSerializer.DeserializeAsync<AiConnectorChatHistoryResponse>(responseStream, _jsonOptions, cancellationToken);
            return result?.History ?? new List<AiChatMessage>();
        }

        private class AiConnectorChatResponse
        {
            [JsonPropertyName("textResponse")]
            public string? TextResponse { get; set; }
        }

        private class AiConnectorChatHistoryResponse
        {
            [JsonPropertyName("history")]
            public List<AiChatMessage>? History { get; set; }
        }
    }
}
