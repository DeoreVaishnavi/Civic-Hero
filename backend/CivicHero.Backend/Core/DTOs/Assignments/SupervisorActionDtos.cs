namespace CivicHero.Backend.Core.DTOs.Assignments;

public sealed class SupervisorActionRequest
{
    public string Reason { get; set; } = string.Empty;
}

public sealed class SupervisorMessageRequest
{
    public string Message { get; set; } = string.Empty;
}

public sealed class EscalationHistoryDto
{
    public long TimelineId { get; init; }
    public long ComplaintId { get; init; }
    public string ReferenceNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ActorName { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}
