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
    private readonly IChatbotService _chatbotService;

    public ChatbotController(IChatbotService chatbotService)
    {
        _chatbotService = chatbotService;
    }

    [HttpPost("session")]
    public async Task<IActionResult> StartSession(CancellationToken cancellationToken) =>
        OkEnvelope("Chat session started.", await _chatbotService.StartSessionAsync(cancellationToken));

    [HttpGet("session/{sessionId}")]
    public async Task<IActionResult> GetSession(string sessionId, CancellationToken cancellationToken) =>
        OkEnvelope("Chat session loaded.", await _chatbotService.GetSessionAsync(sessionId, cancellationToken));

    [HttpPost("message")]
    public async Task<IActionResult> SendMessage([FromBody] ChatMessageRequest request, CancellationToken cancellationToken) =>
        OkEnvelope("Message processed.", await _chatbotService.SendMessageAsync(request, cancellationToken));

    [HttpDelete("session/{sessionId}")]
    public async Task<IActionResult> EndSession(string sessionId, CancellationToken cancellationToken)
    {
        await _chatbotService.EndSessionAsync(sessionId, cancellationToken);
        return Ok(new { success = true, message = "Chat session ended." });
    }

    private IActionResult OkEnvelope<T>(string message, T data) =>
        Ok(new { success = true, message, data });
}
