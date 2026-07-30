using CivicHero.Backend.Core.DTOs.Chat;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CivicHero.Backend.Core.Interfaces
{
    public interface IChatbotService
    {
        Task<ChatSessionResponseDto> CreateSessionAsync(int userId);
        Task<ChatSessionResponseDto> GetSessionByIdAsync(int sessionId, int userId);
        Task<IEnumerable<ChatSessionResponseDto>> GetUserSessionsAsync(int userId);
        Task<ChatSessionResponseDto> UpdateSessionTitleAsync(int sessionId, int userId, UpdateSessionTitleDto request);
        Task<PythonChatResponseDto> SendMessageAsync(int sessionId, int userId, SendMessageRequestDto request);
        Task<IEnumerable<ChatMessageResponseDto>> GetMessagesAsync(int sessionId, int userId);
    }
}