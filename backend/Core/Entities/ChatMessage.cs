using CivicHero.Backend.Core.Common;
using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Entities;

public sealed class ChatMessage : BaseEntity
{
    public long ChatSessionId { get; set; }
    public MessageSender Sender { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset SentAt { get; set; }

    public ChatSession ChatSession { get; set; } = null!;
}
