using CivicHero.Backend.Core.DTOs.Chat;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Repositories
{
    public class ChatMessageRepository : IChatMessageRepository
    {
        private readonly CivicHeroDbContext _context;

        public ChatMessageRepository(CivicHeroDbContext context)
        {
            _context = context;
        }

        public async Task<ChatMessage> AddAsync(ChatMessage chatMessage)
        {
            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();
            return chatMessage;
        }

        public async Task<ChatMessage?> GetByIdAsync(int id)
        {
            return await _context.ChatMessages.FindAsync(id);
        }

        public async Task<IEnumerable<ChatMessage>> GetByUserIdAsync(int userId)
        {
            return await _context.ChatMessages
                .Where(cm => cm.UserId == userId)
                .ToListAsync();
        }

        public async Task<IEnumerable<ChatMessage>> GetByUserIdAndSenderAsync(int userId, MessageSender sender)
        {
            return await _context.ChatMessages
                .Where(cm => cm.UserId == userId && cm.Sender == sender)
                .ToListAsync();
        }

        public async Task<IEnumerable<ChatMessage>> GetBySessionIdAsync(int sessionId)
        {
            return await _context.ChatMessages
                .Where(cm => cm.ChatSessionId == sessionId)
                .OrderBy(cm => cm.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var chatMessage = await _context.ChatMessages.FindAsync(id);
            if (chatMessage == null)
                return false;

            _context.ChatMessages.Remove(chatMessage);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}