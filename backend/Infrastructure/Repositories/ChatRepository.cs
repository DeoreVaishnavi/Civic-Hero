using CivicHero.Backend.Core.DTOs.Chat;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Repositories
{
    public class ChatRepository : IChatRepository
    {
        private readonly CivicHeroDbContext _context;

        public ChatRepository(CivicHeroDbContext context)
        {
            _context = context;
        }

        public async Task<ChatSession> AddAsync(ChatSession chatSession)
        {
            _context.ChatSessions.Add(chatSession);
            await _context.SaveChangesAsync();
            return chatSession;
        }

        public async Task<ChatSession?> GetByIdAsync(int id)
        {
            return await _context.ChatSessions.FindAsync(id);
        }

        public async Task<ChatSession?> GetByUserIdAndIdAsync(int userId, int sessionId)
        {
            return await _context.ChatSessions
                .FirstOrDefaultAsync(cs => cs.Id == sessionId && cs.UserId == userId);
        }

        public async Task<IEnumerable<ChatSession>> GetByUserIdAsync(int userId)
        {
            return await _context.ChatSessions
                .Where(cs => cs.UserId == userId)
                .ToListAsync();
        }

        public async Task<bool> UpdateAsync(ChatSession chatSession)
        {
            _context.ChatSessions.Update(chatSession);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var chatSession = await _context.ChatSessions.FindAsync(id);
            if (chatSession == null)
                return false;

            _context.ChatSessions.Remove(chatSession);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}