using CivicHero.Backend.Core.Common;
using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Entities;

/// <summary>
/// Represents a single message within a chat session.
/// </summary>
public sealed class ChatMessage : AuditableEntity
{
    /// <summary>
    /// Related chat session identifier.
    /// </summary>
    public Guid ChatSessionId { get; private set; }

    /// <summary>
    /// Navigation property.
    /// </summary>
    public ChatSession ChatSession { get; private set; }

    /// <summary>
    /// Identifies who sent the message.
    /// </summary>
    public MessageSender Sender { get; private set; }

    /// <summary>
    /// Message content.
    /// </summary>
    public string Message { get; private set; }

    /// <summary>
    /// UTC timestamp when the message was sent.
    /// </summary>
    public DateTime SentOnUtc { get; private set; }

 private ChatMessage()
{
    ChatSession = null!;

    Message = string.Empty;
}

    /// <summary>
    /// Creates a new chat message.
    /// </summary>
    public ChatMessage(
        Guid chatSessionId,
        MessageSender sender,
        string message)
    {
        if (chatSessionId == Guid.Empty)
            throw new ArgumentException("Chat session ID is required.", nameof(chatSessionId));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty.", nameof(message));

        ChatSessionId = chatSessionId;
        Sender = sender;
        Message = message.Trim();
        SentOnUtc = DateTime.UtcNow;
    }
}