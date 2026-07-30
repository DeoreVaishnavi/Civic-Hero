using CivicHero.Backend.Core.DTOs.Chat;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CivicHero.Backend.Core.Services
{
    public class ChatMessageService : IChatMessageService
    {
        private readonly IChatMessageRepository _chatMessageRepository;

        public ChatMessageService(IChatMessageRepository chatMessageRepository)
        {
            _chatMessageRepository = chatMessageRepository;
        }

        public async Task<ChatMessage> SendUserMessageAsync(int userId, string content)
        {
            var message = new ChatMessage
            {
                UserId = userId,
                Content = content,
                Sender = MessageSender.User
            };

            return await _chatMessageRepository.AddAsync(message);
        }

        public async Task<ChatMessage> SendBotMessageAsync(int userId, string content)
        {
            var message = new ChatMessage
            {
                UserId = userId,
                Content = content,
                Sender = MessageSender.Assistant
            };

            return await _chatMessageRepository.AddAsync(message);
        }

        public async Task<IEnumerable<ChatMessage>> GetChatHistoryAsync(int userId)
        {
            return await _chatMessageRepository.GetByUserIdAsync(userId);
        }

        public async Task<IEnumerable<ChatMessage>> GetBotMessagesAsync(int userId)
        {
            return await _chatMessageRepository.GetByUserIdAndSenderAsync(userId, MessageSender.Assistant);
        }

        public async Task<bool> DeleteMessageAsync(int messageId, int userId)
        {
            // Optionally, we can check if the message belongs to the user before deleting
            var message = await _chatMessageRepository.GetByIdAsync(messageId);
            if (message == null || message.UserId != userId)
                return false;

            return await _chatMessageRepository.DeleteAsync(messageId);
        }
    }
}