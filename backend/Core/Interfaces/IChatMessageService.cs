using CivicHero.Backend.Core.DTOs.Chat;
using CivicHero.Backend.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CivicHero.Backend.Core.Interfaces
{
    public interface IChatMessageService
    {
        Task<ChatMessage> SendUserMessageAsync(int userId, string content);
        Task<ChatMessage> SendBotMessageAsync(int userId, string content);
        Task<IEnumerable<ChatMessage>> GetChatHistoryAsync(int userId);
        Task<IEnumerable<ChatMessage>> GetBotMessagesAsync(int userId);
        Task<bool> DeleteMessageAsync(int messageId, int userId);
    }
}