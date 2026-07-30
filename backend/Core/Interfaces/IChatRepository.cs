using CivicHero.Backend.Core.DTOs.Chat;
using CivicHero.Backend.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CivicHero.Backend.Core.Interfaces
{
    public interface IChatRepository
    {
        Task<ChatSession> AddAsync(ChatSession chatSession);
        Task<ChatSession?> GetByIdAsync(int id);
        Task<ChatSession?> GetByUserIdAndIdAsync(int userId, int sessionId);
        Task<IEnumerable<ChatSession>> GetByUserIdAsync(int userId);
        Task<bool> UpdateAsync(ChatSession chatSession);
        Task<bool> DeleteAsync(int id);
    }
}