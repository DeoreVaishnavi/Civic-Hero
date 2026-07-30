using CivicHero.Backend.Core.DTOs.Chat;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.AI; // Added for IPythonChatbotClient
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CivicHero.Backend.Core.Services
{
    public class ChatbotService : IChatbotService
    {
        private readonly IChatSessionRepository _sessionRepository;
        private readonly IChatMessageRepository _messageRepository;
        private readonly IComplaintRepository _complaintRepository;
        private readonly IPythonChatbotClient _pythonClient;

        public ChatbotService(
            IChatSessionRepository sessionRepository,
            IChatMessageRepository messageRepository,
            IComplaintRepository complaintRepository,
            IPythonChatbotClient pythonClient)
        {
            _sessionRepository = sessionRepository;
            _messageRepository = messageRepository;
            _complaintRepository = complaintRepository;
            _pythonClient = pythonClient;
        }

        public async Task<ChatSessionResponseDto> CreateSessionAsync(int userId)
        {
            var session = new ChatSession
            {
                UserId = userId,
                Title = "New Chat",
                IsActive = true
            };

            var createdSession = await _sessionRepository.AddAsync(session);
            return new ChatSessionResponseDto
            {
                Id = createdSession.Id,
                Title = createdSession.Title,
                CreatedAt = createdSession.CreatedAt,
                UpdatedAt = createdSession.UpdatedAt
            };
        }

        public async Task<ChatSessionResponseDto> GetSessionByIdAsync(int sessionId, int userId)
        {
            var session = await _sessionRepository.GetByIdAndUserIdAsync(sessionId, userId);
            if (session == null)
                return null;

            return new ChatSessionResponseDto
            {
                Id = session.Id,
                Title = session.Title,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt
            };
        }

        public async Task<IEnumerable<ChatSessionResponseDto>> GetUserSessionsAsync(int userId)
        {
            var sessions = await _sessionRepository.GetByUserIdAsync(userId);
            return sessions.Select(s => new ChatSessionResponseDto
            {
                Id = s.Id,
                Title = s.Title,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            });
        }

        public async Task<ChatSessionResponseDto> UpdateSessionTitleAsync(int sessionId, int userId, UpdateSessionTitleDto request)
        {
            var session = await _sessionRepository.GetByIdAndUserIdAsync(sessionId, userId);
            if (session == null)
                return null;

            session.Title = request.Title ?? session.Title;
            session.UpdatedAt = DateTime.UtcNow;

            var updatedSession = await _sessionRepository.UpdateAsync(session);
            return new ChatSessionResponseDto
            {
                Id = updatedSession.Id,
                Title = updatedSession.Title,
                CreatedAt = updatedSession.CreatedAt,
                UpdatedAt = updatedSession.UpdatedAt
            };
        }

        public async Task<PythonChatResponseDto> SendMessageAsync(int sessionId, int userId, SendMessageRequestDto request)
        {
            // Validate session ownership
            var session = await _sessionRepository.GetByIdAndUserIdAsync(sessionId, userId);
            if (session == null)
                return new PythonChatResponseDto
                {
                    Success = false,
                    ErrorMessage = "Session not found or access denied."
                };

            // If complaintId is provided, verify ownership
            if (request.ComplaintId.HasValue)
            {
                var complaint = await _complaintRepository.GetByIdAsync(request.ComplaintId.Value);
                if (complaint == null || complaint.UserId != userId)
                {
                    return new PythonChatResponseDto
                    {
                        Success = false,
                        ErrorMessage = "Complaint not found or access denied."
                    };
                }
            }

            // Save user message
            var userMessage = new ChatMessage
            {
                UserId = userId,
                ChatSessionId = sessionId,
                Content = request.Content,
                Sender = MessageSender.User
            };
            await _messageRepository.AddAsync(userMessage);

            // Prepare request to Python service
            var pythonRequest = new PythonChatRequestDto
            {
                Message = request.Content,
                UserId = userId,
                ComplaintId = request.ComplaintId,
                SessionId = sessionId.ToString()
            };

            // Call Python service
            var pythonResponse = await _pythonClient.SendMessageAsync(pythonRequest);

            if (!pythonResponse.Success)
            {
                // Optionally, we could still save a bot message indicating error?
                // For now, we return the error.
                return pythonResponse;
            }

            // Save bot response
            var botMessage = new ChatMessage
            {
                UserId = userId,
                ChatSessionId = sessionId,
                Content = pythonResponse.Response ?? string.Empty,
                Sender = MessageSender.Assistant
            };
            await _messageRepository.AddAsync(botMessage);

            return pythonResponse;
        }

        public async Task<IEnumerable<ChatMessageResponseDto>> GetMessagesAsync(int sessionId, int userId)
        {
            // Verify session ownership
            var session = await _sessionRepository.GetByIdAndUserIdAsync(sessionId, userId);
            if (session == null)
                return Enumerable.Empty<ChatMessageResponseDto>();

            var messages = await _messageRepository.GetBySessionIdAsync(sessionId);

            return messages.Select(m => new ChatMessageResponseDto
            {
                Id = m.Id,
                UserId = m.UserId,
                Content = m.Content,
                CreatedAt = m.CreatedAt,
                Sender = m.Sender,
                Username = null // We don't have the username here; we could join with User table if needed.
            });
        }
    }
}