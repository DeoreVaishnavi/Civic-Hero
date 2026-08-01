using System.Text.Json;
using System.Text.RegularExpressions;
using CivicHero.Backend.Core.DTOs.Chatbot;
using CivicHero.Backend.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/chatbot")]
[Authorize]
public sealed class ChatbotController : ControllerBase
{
    private static readonly JsonSerializerOptions StreamJsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IChatbotService _chatbotService;

    public ChatbotController(IChatbotService chatbotService)
    {
        _chatbotService = chatbotService;
    }

    [HttpPost("session")]
    public async Task<IActionResult> StartSession(CancellationToken cancellationToken) =>
        OkEnvelope("Chat session started.", await _chatbotService.StartSessionAsync(cancellationToken));

    [HttpGet("sessions")]
    public async Task<IActionResult> ListSessions(
        [FromQuery] string? search,
        [FromQuery] int take = 30,
        CancellationToken cancellationToken = default) =>
        OkEnvelope("Recent chat sessions loaded.", await _chatbotService.ListSessionsAsync(search, take, cancellationToken));

    [HttpGet("session/{sessionId}")]
    public async Task<IActionResult> GetSession(string sessionId, CancellationToken cancellationToken) =>
        OkEnvelope("Chat session loaded.", await _chatbotService.GetSessionAsync(sessionId, cancellationToken));

    [HttpPut("session/{sessionId}/title")]
    public async Task<IActionResult> RenameSession(
        string sessionId,
        [FromBody] RenameChatSessionRequest request,
        CancellationToken cancellationToken) =>
        OkEnvelope("Chat session renamed.", await _chatbotService.RenameSessionAsync(sessionId, request, cancellationToken));

    [HttpPost("session/{sessionId}/continue")]
    public async Task<IActionResult> ContinueSession(string sessionId, CancellationToken cancellationToken) =>
        OkEnvelope("Chat session continued.", await _chatbotService.ContinueSessionAsync(sessionId, cancellationToken));

    [HttpPost("message")]
    public async Task<IActionResult> SendMessage([FromBody] ChatMessageRequest request, CancellationToken cancellationToken) =>
        OkEnvelope("Message processed.", await _chatbotService.SendMessageAsync(request, cancellationToken));

    [HttpPost("message/stream")]
    public async Task StreamMessage([FromBody] ChatMessageRequest request, CancellationToken cancellationToken)
    {
        // Generate and persist the complete response before starting the HTTP stream so
        // normal API exception handling still applies if validation or authorization fails.
        var result = await _chatbotService.SendMessageAsync(request, cancellationToken);

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "application/x-ndjson; charset=utf-8";
        Response.Headers["Cache-Control"] = "no-cache, no-transform";
        Response.Headers.Append("X-Accel-Buffering", "no");

        await WriteStreamEventAsync("user", result.UserMessage, cancellationToken);
        await WriteStreamEventAsync("meta", new
        {
            result.SessionId,
            result.Intent,
            result.Confidence,
            messageId = result.AssistantMessage.Id,
            result.AssistantMessage.SentAt,
            result.AssistantMessage.ActionLabel,
            result.AssistantMessage.ActionUrl
        }, cancellationToken);

        foreach (Match match in Regex.Matches(result.AssistantMessage.Content, @"\S+\s*"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await WriteStreamEventAsync("token", new { value = match.Value }, cancellationToken);
        }

        await WriteStreamEventAsync("done", result.AssistantMessage, cancellationToken);
    }

    [HttpPost("session/{sessionId}/messages/{messageId:long}/feedback")]
    public async Task<IActionResult> SubmitFeedback(
        string sessionId,
        long messageId,
        [FromBody] ChatMessageFeedbackRequest request,
        CancellationToken cancellationToken) =>
        OkEnvelope(
            "Chat response feedback recorded.",
            await _chatbotService.SubmitFeedbackAsync(sessionId, messageId, request, cancellationToken));

    [HttpDelete("session/{sessionId}")]
    public async Task<IActionResult> EndSession(string sessionId, CancellationToken cancellationToken)
    {
        await _chatbotService.EndSessionAsync(sessionId, cancellationToken);
        return Ok(new { success = true, message = "Chat session ended." });
    }

    [HttpDelete("sessions/{sessionId}")]
    public async Task<IActionResult> DeleteSession(string sessionId, CancellationToken cancellationToken)
    {
        await _chatbotService.DeleteSessionAsync(sessionId, cancellationToken);
        return Ok(new { success = true, message = "Chat session permanently deleted." });
    }

    private async Task WriteStreamEventAsync(string type, object data, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(new { type, data }, StreamJsonOptions);
        await Response.WriteAsync($"{json}\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    private IActionResult OkEnvelope<T>(string message, T data) =>
        Ok(new { success = true, message, data });
}
