using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CivicHero.Backend.Core.Services;

namespace CivicHero.Backend.Infrastructure.AI;

public sealed class PythonChatbotClient : IPythonChatbotClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PythonChatbotClient> _logger;

    public PythonChatbotClient(HttpClient httpClient, ILogger<PythonChatbotClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PythonChatbotReply?> GenerateAsync(string message, string conversationId, long userId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync("api/v1/chat/generate", new
            {
                message,
                conversation_id = conversationId,
                user_id = userId.ToString(),
                include_context = true
            }, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Python chatbot returned HTTP {StatusCode}; using the local fallback.", response.StatusCode);
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<Response>(cancellationToken: cancellationToken);
            return string.IsNullOrWhiteSpace(payload?.Message)
                ? null
                : new PythonChatbotReply(payload.Message, Math.Clamp(payload.ConfidenceScore ?? 0.6m, 0m, 1m));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            _logger.LogWarning(exception, "Python chatbot is unavailable; using the local fallback.");
            return null;
        }
    }

    private sealed class Response
    {
        [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
        [JsonPropertyName("confidence_score")] public decimal? ConfidenceScore { get; set; }
    }
}
