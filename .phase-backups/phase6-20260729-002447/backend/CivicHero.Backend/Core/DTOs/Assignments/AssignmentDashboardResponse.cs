namespace CivicHero.Backend.Core.DTOs.Assignments;

public sealed class AssignmentDashboardResponse
{
    public int Pending { get; init; }
    public int InProgress { get; init; }
    public int CompletedThisWeek { get; init; }
    public int Overdue { get; init; }
    public int Unassigned { get; init; }
    public int ReassignmentPending { get; init; }
    public decimal SlaCompliancePercent { get; init; }
    public IReadOnlyList<AssignmentDto> PriorityItems { get; init; } = Array.Empty<AssignmentDto>();
}
