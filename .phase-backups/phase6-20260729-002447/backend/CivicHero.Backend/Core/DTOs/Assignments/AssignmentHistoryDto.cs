namespace CivicHero.Backend.Core.DTOs.Assignments;

public sealed class AssignmentHistoryDto
{
    public long Id { get; init; }
    public long OfficerId { get; init; }
    public string OfficerName { get; init; } = string.Empty;
    public string AssignedByName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? Reason { get; init; }
    public DateTimeOffset AssignedAt { get; init; }
    public DateTimeOffset? RespondedAt { get; init; }
    public DateTimeOffset? ReassignedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public DateTimeOffset AssignmentDueAt { get; init; }
    public DateTimeOffset ResolutionDueAt { get; init; }
    public bool IsCurrent { get; init; }
}
