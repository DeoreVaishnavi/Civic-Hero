using CivicHero.Backend.Core.DTOs.Chat;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CivicHero.Backend.Core.Interfaces
{
    public interface IChatMessageRepository
    {
        Task<ChatMessage> AddAsync(ChatMessage chatMessage);
        Task<ChatMessage?> GetByIdAsync(int id);
        Task<IEnumerable<ChatMessage>> GetByUserIdAsync(int userId);
        Task<IEnumerable<ChatMessage>> GetByUserIdAndSenderAsync(int userId, MessageSender sender);
        Task<IEnumerable<ChatMessage>> GetBySessionIdAsync(int sessionId);
        Task<bool> DeleteAsync(int id);
    }
}