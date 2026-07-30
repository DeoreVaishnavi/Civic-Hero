using CivicHero.Backend.Core.DTOs.Chat;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CivicHero.Backend.Infrastructure.Repositories
{
    public class ChatSessionRepository : IChatSessionRepository
    {
        private readonly CivicHeroDbContext _context;

        public ChatSessionRepository(CivicHeroDbContext context)
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

        public async Task<ChatSession?> GetByIdAndUserIdAsync(int id, int userId)
        {
            return await _context.ChatSessions
                .FirstOrDefaultAsync(cs => cs.Id == id && cs.UserId == userId);
        }

        public async Task<IEnumerable<ChatSession>> GetByUserIdAsync(int userId)
        {
            return await _context.ChatSessions
                .Where(cs => cs.UserId == userId)
                .ToListAsync();
        }

        public async Task<ChatSession> UpdateAsync(ChatSession chatSession)
        {
            _context.ChatSessions.Update(chatSession);
            await _context.SaveChangesAsync();
            return chatSession;
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