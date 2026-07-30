using CivicHero.Backend.Core.DTOs.Chat;
using CivicHero.Backend.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers
{
    [ApiController]
    [Route("api/chatbot")]
    [Authorize]
    public class ChatbotController : ControllerBase
    {
        private readonly IChatbotService _chatbotService;

        public ChatbotController(IChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        // POST: api/chatbot/sessions
        [HttpPost("sessions")]
        public async Task<ActionResult<ChatSessionResponseDto>> CreateSession()
        {
            // Get user ID from JWT claims
            var userIdClaim = User.FindFirst("UserId") ?? User.FindFirst("sub");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized();
            }

            var session = await _chatbotService.CreateSessionAsync(userId);
            return Ok(session);
        }

        // GET: api/chatbot/sessions
        [HttpGet("sessions")]
        public async Task<ActionResult<IEnumerable<ChatSessionResponseDto>>> GetUserSessions()
        {
            var userIdClaim = User.FindFirst("UserId") ?? User.FindFirst("sub");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized();
            }

            var sessions = await _chatbotService.GetUserSessionsAsync(userId);
            return Ok(sessions);
        }

        // GET: api/chatbot/sessions/{sessionId}
        [HttpGet("sessions/{sessionId}")]
        public async Task<ActionResult<ChatSessionResponseDto>> GetSessionById(int sessionId)
        {
            var userIdClaim = User.FindFirst("UserId") ?? User.FindFirst("sub");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized();
            }

            var session = await _chatbotService.GetSessionByIdAsync(sessionId, userId);
            if (session == null)
            {
                return NotFound();
            }

            return Ok(session);
        }

        // PUT: api/chatbot/sessions/{sessionId}/title
        [HttpPut("sessions/{sessionId}/title")]
        public async Task<ActionResult<ChatSessionResponseDto>> UpdateSessionTitle(int sessionId, [FromBody] UpdateSessionTitleDto request)
        {
            var userIdClaim = User.FindFirst("UserId") ?? User.FindFirst("sub");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized();
            }

            var session = await _chatbotService.UpdateSessionTitleAsync(sessionId, userId, request);
            if (session == null)
            {
                return NotFound();
            }

            return Ok(session);
        }

        // POST: api/chatbot/sessions/{sessionId}/messages
        [HttpPost("sessions/{sessionId}/messages")]
        public async Task<ActionResult<PythonChatResponseDto>> SendMessage(int sessionId, [FromBody] SendMessageRequestDto request)
        {
            var userIdClaim = User.FindFirst("UserId") ?? User.FindFirst("sub");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized();
            }

            var response = await _chatbotService.SendMessageAsync(sessionId, userId, request);
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        // GET: api/chatbot/sessions/{sessionId}/messages
        [HttpGet("sessions/{sessionId}/messages")]
        public async Task<ActionResult<IEnumerable<ChatMessageResponseDto>>> GetMessages(int sessionId)
        {
            var userIdClaim = User.FindFirst("UserId") ?? User.FindFirst("sub");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized();
            }

            var messages = await _chatbotService.GetMessagesAsync(sessionId, userId);
            return Ok(messages);
        }
    }
}