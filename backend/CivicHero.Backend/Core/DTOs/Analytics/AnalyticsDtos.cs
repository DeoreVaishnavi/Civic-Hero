namespace CivicHero.Backend.Core.DTOs.Analytics;

public sealed class AnalyticsFilter
{
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public long? DepartmentId { get; init; }
    public long? WardId { get; init; }
}

public sealed record AnalyticsPeriod(DateTimeOffset From, DateTimeOffset To, int Days);
public sealed record MetricSlice(string Name, int Count, decimal Percentage);
public sealed record TrendPoint(string Period, int Created, int Closed, int Escalated);

public sealed record AnalyticsOverviewResponse(
    AnalyticsPeriod Period,
    int TotalComplaints,
    int OpenComplaints,
    int ClosedComplaints,
    decimal ResolutionRate,
    decimal AverageResolutionHours,
    decimal SlaCompliance,
    decimal SatisfactionScore,
    int RegisteredUsers,
    int ActiveUsers,
    int ActiveDepartments,
    int ActiveWards,
    DateTimeOffset GeneratedAt);

public sealed record ComplaintAnalyticsResponse(
    AnalyticsPeriod Period,
    int Total,
    int Open,
    int Closed,
    int Escalated,
    int FraudClosed,
    decimal ResolutionRate,
    IReadOnlyList<MetricSlice> ByStatus,
    IReadOnlyList<MetricSlice> ByCategory,
    IReadOnlyList<MetricSlice> ByPriority,
    IReadOnlyList<TrendPoint> MonthlyTrend);

public sealed record DepartmentAnalyticsRow(
    long DepartmentId,
    string DepartmentName,
    int Total,
    int Open,
    int Closed,
    int Escalated,
    decimal ResolutionRate,
    decimal AverageResolutionHours,
    decimal SlaCompliance,
    decimal SatisfactionScore,
    int Rank);

public sealed record OfficerAnalyticsRow(
    long OfficerId,
    string OfficerName,
    string? DepartmentName,
    string? WardName,
    int Assigned,
    int Active,
    int Completed,
    decimal CompletionRate,
    decimal AverageCompletionHours,
    decimal SlaCompliance,
    decimal RevisitRate);

public sealed record WardAnalyticsRow(
    long WardId,
    string WardName,
    string DepartmentName,
    int Total,
    int Open,
    int HighPriority,
    int Critical,
    int Closed,
    decimal ResolutionRate,
    decimal HeatScore,
    decimal Latitude,
    decimal Longitude);

public sealed record SlaAnalyticsResponse(
    AnalyticsPeriod Period,
    int TotalAssignments,
    int AssignmentCompliant,
    int ResolutionCompliant,
    int CurrentlyOverdue,
    decimal AssignmentComplianceRate,
    decimal ResolutionComplianceRate,
    decimal OverallComplianceRate,
    IReadOnlyList<MetricSlice> ByPriority);

public sealed record SatisfactionAnalyticsResponse(
    AnalyticsPeriod Period,
    int RatingCount,
    decimal AverageRating,
    decimal ApprovalRate,
    decimal DisputeRate,
    IReadOnlyList<MetricSlice> RatingDistribution);

public sealed record HeatmapPointResponse(
    decimal Latitude,
    decimal Longitude,
    int ComplaintCount,
    decimal HeatScore,
    string DominantCategory,
    string RiskLevel,
    long? WardId,
    string? WardName);

public sealed record AnalyticsExportResult(byte[] Content, string ContentType, string FileName);
