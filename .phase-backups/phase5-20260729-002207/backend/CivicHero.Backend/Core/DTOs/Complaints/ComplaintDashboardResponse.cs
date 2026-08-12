namespace CivicHero.Backend.Core.DTOs.Complaints;

public sealed class ComplaintDashboardResponse
{
    public int Total { get; init; }
    public int Open { get; init; }
    public int InProgress { get; init; }
    public int Resolved { get; init; }
    public int Closed { get; init; }
    public int Withdrawn { get; init; }
    public int TotalUpvotes { get; init; }
    public IReadOnlyList<ComplaintResponse> RecentComplaints { get; init; } = Array.Empty<ComplaintResponse>();
}
