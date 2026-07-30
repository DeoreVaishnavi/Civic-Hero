using System;

namespace CivicHero.Backend.Core.DTOs.Chat
{
    public class CreateChatMessageDto
    {
        public int UserId { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsBot { get; set; } = false;
    }
}