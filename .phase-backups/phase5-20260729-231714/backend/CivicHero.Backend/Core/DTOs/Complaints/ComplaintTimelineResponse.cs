namespace CivicHero.Backend.Core.DTOs.Complaints;

public sealed class ComplaintTimelineResponse
{
    public long Id { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ActorName { get; init; } = "System";
    public DateTimeOffset Timestamp { get; init; }
}
