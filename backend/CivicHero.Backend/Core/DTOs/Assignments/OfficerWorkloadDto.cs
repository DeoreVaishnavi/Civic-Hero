namespace CivicHero.Backend.Core.DTOs.Assignments;

public sealed class OfficerWorkloadDto
{
    public long OfficerId { get; init; }
    public string OfficerName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public long? DepartmentId { get; init; }
    public string? DepartmentName { get; init; }
    public long? WardId { get; init; }
    public string? WardName { get; init; }
    public int PendingCount { get; init; }
    public int InProgressCount { get; init; }
    public int OverdueCount { get; init; }
    public int CompletedCount { get; init; }
    public int ActiveWorkload => PendingCount + InProgressCount;
}

public sealed class EligibleOfficerDto
{
    public long OfficerId { get; init; }
    public string OfficerName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = string.Empty;
    public string WardName { get; init; } = string.Empty;
    public int ActiveWorkload { get; init; }
    public int OverdueCount { get; init; }
    public string Recommendation { get; init; } = string.Empty;
}
