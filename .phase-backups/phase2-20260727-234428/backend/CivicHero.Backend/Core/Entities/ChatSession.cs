using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Entities;

public sealed class ChatSession : BaseEntity
{
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public string SessionId { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndedAt { get; set; }
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
