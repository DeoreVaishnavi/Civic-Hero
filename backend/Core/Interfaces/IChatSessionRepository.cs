using CivicHero.Backend.Core.DTOs.Chat;
using CivicHero.Backend.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CivicHero.Backend.Core.Interfaces
{
    public interface IChatSessionRepository
    {
        Task<ChatSession> AddAsync(ChatSession chatSession);
        Task<ChatSession?> GetByIdAsync(int id);
        Task<ChatSession?> GetByIdAndUserIdAsync(int id, int userId);
        Task<IEnumerable<ChatSession>> GetByUserIdAsync(int userId);
        Task<ChatSession> UpdateAsync(ChatSession chatSession);
        Task<bool> DeleteAsync(int id);
    }
}