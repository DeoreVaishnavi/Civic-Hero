using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using CivicHero.Backend.Core.DTOs.Analytics;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Core.Services;

public sealed class AnalyticsService : IAnalyticsService
{
    private static readonly ComplaintStatus[] ClosedStatuses =
    [
        ComplaintStatus.Closed,
        ComplaintStatus.ClosedAuto
    ];

    private static readonly ComplaintStatus[] TerminalStatuses =
    [
        ComplaintStatus.Closed,
        ComplaintStatus.ClosedAuto,
        ComplaintStatus.ClosedFraud,
        ComplaintStatus.Withdrawn,
        ComplaintStatus.Merged
    ];

    private static readonly ComplaintStatus[] PublicHeatmapStatuses =
    [
        ComplaintStatus.Assigned,
        ComplaintStatus.ReassignmentPending,
        ComplaintStatus.InProgress,
        ComplaintStatus.Escalated,
        ComplaintStatus.Resolved,
        ComplaintStatus.VerificationPending,
        ComplaintStatus.Disputed,
        ComplaintStatus.Appealed,
        ComplaintStatus.Closed,
        ComplaintStatus.ClosedAuto
    ];

    private readonly CivicDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AnalyticsService(CivicDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AnalyticsOverviewResponse> GetOverviewAsync(AnalyticsFilter filter, CancellationToken cancellationToken = default)
    {
        var (period, complaints) = await LoadComplaintsAsync(filter, cancellationToken);
        var total = complaints.Count;
        var closed = complaints.Count(c => ClosedStatuses.Contains(c.Status));
        var open = complaints.Count(c => !TerminalStatuses.Contains(c.Status));
        var resolutionHours = complaints
            .Where(c => c.ClosedAt.HasValue || c.ResolvedAt.HasValue)
            .Select(c => ((c.ClosedAt ?? c.ResolvedAt)!.Value - c.CreatedAt).TotalHours)
            .Where(value => value >= 0)
            .ToList();

        var sla = await BuildSlaAsync(period, complaints, cancellationToken);
        var satisfaction = await BuildSatisfactionAsync(period, complaints, cancellationToken);
        var usersQuery = _db.Users.AsNoTracking().Where(user => !user.IsDeleted);
        var activeUsers = await usersQuery.CountAsync(user => user.IsActive, cancellationToken);
        var totalUsers = await usersQuery.CountAsync(cancellationToken);
        var departments = await _db.Departments.AsNoTracking().CountAsync(item => !item.IsDeleted && item.IsActive, cancellationToken);
        var wards = await _db.Wards.AsNoTracking().CountAsync(item => !item.IsDeleted && item.IsActive, cancellationToken);

        return new AnalyticsOverviewResponse(
            period,
            total,
            open,
            closed,
            Percent(closed, total),
            Round(resolutionHours.Count == 0 ? 0 : (decimal)resolutionHours.Average()),
            sla.OverallComplianceRate,
            satisfaction.AverageRating,
            totalUsers,
            activeUsers,
            departments,
            wards,
            DateTimeOffset.UtcNow);
    }

    public async Task<ComplaintAnalyticsResponse> GetComplaintAnalyticsAsync(AnalyticsFilter filter, CancellationToken cancellationToken = default)
    {
        var (period, complaints) = await LoadComplaintsAsync(filter, cancellationToken);
        var total = complaints.Count;
        var closed = complaints.Count(c => ClosedStatuses.Contains(c.Status));
        var open = complaints.Count(c => !TerminalStatuses.Contains(c.Status));
        var escalated = complaints.Count(c => c.Status == ComplaintStatus.Escalated || c.Status == ComplaintStatus.Appealed);
        var fraudClosed = complaints.Count(c => c.Status == ComplaintStatus.ClosedFraud);

        return new ComplaintAnalyticsResponse(
            period,
            total,
            open,
            closed,
            escalated,
            fraudClosed,
            Percent(closed, total),
            Slice(complaints.GroupBy(c => c.Status.ToString()).Select(g => (g.Key, g.Count())), total),
            Slice(complaints.GroupBy(c => string.IsNullOrWhiteSpace(c.Category) ? "Uncategorised" : c.Category).Select(g => (g.Key, g.Count())), total),
            Slice(complaints.GroupBy(c => c.Priority.ToString()).Select(g => (g.Key, g.Count())), total),
            BuildTrend(period, complaints));
    }

    public async Task<IReadOnlyList<DepartmentAnalyticsRow>> GetDepartmentAnalyticsAsync(AnalyticsFilter filter, CancellationToken cancellationToken = default)
    {
        var (period, complaints) = await LoadComplaintsAsync(filter, cancellationToken);
        var assignments = await LoadAssignmentsAsync(complaints, cancellationToken);
        var satisfaction = await LoadRatingsAsync(complaints, cancellationToken);
        var rows = complaints.GroupBy(c => new { c.DepartmentId, Name = c.Department?.Name ?? $"Department {c.DepartmentId}" })
            .Select(group =>
            {
                var items = group.ToList();
                var closed = items.Count(c => ClosedStatuses.Contains(c.Status));
                var resolutionHours = items.Where(c => c.ClosedAt.HasValue || c.ResolvedAt.HasValue)
                    .Select(c => ((c.ClosedAt ?? c.ResolvedAt)!.Value - c.CreatedAt).TotalHours)
                    .Where(value => value >= 0)
                    .ToList();
                var complaintIds = items.Select(c => c.Id).ToHashSet();
                var deptAssignments = assignments.Where(a => complaintIds.Contains(a.ComplaintId)).ToList();
                var completedDue = deptAssignments.Where(a => a.CompletedAt.HasValue).ToList();
                var sla = completedDue.Count == 0 ? 0 : Percent(completedDue.Count(a => a.CompletedAt!.Value <= a.ResolutionDueAt), completedDue.Count);
                var ratings = satisfaction.Where(item => complaintIds.Contains(item.ComplaintId)).Select(item => item.Rating).Where(r => r > 0).ToList();
                return new DepartmentAnalyticsRow(
                    group.Key.DepartmentId,
                    group.Key.Name,
                    items.Count,
                    items.Count(c => !TerminalStatuses.Contains(c.Status)),
                    closed,
                    items.Count(c => c.Status == ComplaintStatus.Escalated || c.Status == ComplaintStatus.Appealed),
                    Percent(closed, items.Count),
                    Round(resolutionHours.Count == 0 ? 0 : (decimal)resolutionHours.Average()),
                    sla,
                    Round(ratings.Count == 0 ? 0 : (decimal)ratings.Average()),
                    0);
            })
            .OrderByDescending(row => row.ResolutionRate)
            .ThenByDescending(row => row.SatisfactionScore)
            .ThenBy(row => row.AverageResolutionHours)
            .ToList();

        return rows.Select((row, index) => row with { Rank = index + 1 }).ToList();
    }

    public async Task<IReadOnlyList<OfficerAnalyticsRow>> GetOfficerAnalyticsAsync(AnalyticsFilter filter, CancellationToken cancellationToken = default)
    {
        var (_, complaints) = await LoadComplaintsAsync(filter, cancellationToken);
        var complaintIds = complaints.Select(c => c.Id).ToHashSet();
        var assignments = await _db.ComplaintAssignments.AsNoTracking()
            .Include(a => a.Officer).ThenInclude(o => o.Department)
            .Include(a => a.Officer).ThenInclude(o => o.Ward)
            .Where(a => complaintIds.Contains(a.ComplaintId))
            .ToListAsync(cancellationToken);

        return assignments.GroupBy(a => new
            {
                a.OfficerId,
                Name = a.Officer?.FullName ?? $"Officer {a.OfficerId}",
                Department = a.Officer?.Department?.Name,
                Ward = a.Officer?.Ward?.Name
            })
            .Select(group =>
            {
                var items = group.ToList();
                var completed = items.Where(a => a.CompletedAt.HasValue).ToList();
                var completionHours = completed.Select(a => (a.CompletedAt!.Value - a.AssignedAt).TotalHours).Where(value => value >= 0).ToList();
                var officerComplaintIds = items.Select(a => a.ComplaintId).ToHashSet();
                var disputed = complaints.Count(c => officerComplaintIds.Contains(c.Id) && (c.Status == ComplaintStatus.Disputed || c.Status == ComplaintStatus.Appealed));
                return new OfficerAnalyticsRow(
                    group.Key.OfficerId,
                    group.Key.Name,
                    group.Key.Department,
                    group.Key.Ward,
                    items.Count,
                    items.Count(a => a.IsCurrent && !a.CompletedAt.HasValue),
                    completed.Count,
                    Percent(completed.Count, items.Count),
                    Round(completionHours.Count == 0 ? 0 : (decimal)completionHours.Average()),
                    completed.Count == 0 ? 0 : Percent(completed.Count(a => a.CompletedAt!.Value <= a.ResolutionDueAt), completed.Count),
                    Percent(disputed, officerComplaintIds.Count));
            })
            .OrderByDescending(row => row.SlaCompliance)
            .ThenByDescending(row => row.Completed)
            .ThenBy(row => row.AverageCompletionHours)
            .ToList();
    }

    public async Task<IReadOnlyList<WardAnalyticsRow>> GetWardAnalyticsAsync(AnalyticsFilter filter, CancellationToken cancellationToken = default)
    {
        var (_, complaints) = await LoadComplaintsAsync(filter, cancellationToken);
        return complaints.GroupBy(c => new
            {
                c.WardId,
                WardName = c.Ward?.Name ?? $"Ward {c.WardId}",
                DepartmentName = c.Department?.Name ?? "Unknown",
                North = c.Ward?.BoundaryNorth ?? c.Latitude,
                South = c.Ward?.BoundarySouth ?? c.Latitude,
                East = c.Ward?.BoundaryEast ?? c.Longitude,
                West = c.Ward?.BoundaryWest ?? c.Longitude
            })
            .Select(group =>
            {
                var items = group.ToList();
                var closed = items.Count(c => ClosedStatuses.Contains(c.Status));
                var open = items.Count(c => !TerminalStatuses.Contains(c.Status));
                var high = items.Count(c => c.Priority == ComplaintPriority.High);
                var critical = items.Count(c => c.Priority == ComplaintPriority.Critical);
                return new WardAnalyticsRow(
                    group.Key.WardId,
                    group.Key.WardName,
                    group.Key.DepartmentName,
                    items.Count,
                    open,
                    high,
                    critical,
                    closed,
                    Percent(closed, items.Count),
                    Round(open + (high * 2m) + (critical * 3m)),
                    Round((group.Key.North + group.Key.South) / 2m, 6),
                    Round((group.Key.East + group.Key.West) / 2m, 6));
            })
            .OrderByDescending(row => row.HeatScore)
            .ThenByDescending(row => row.Total)
            .ToList();
    }

    public async Task<SlaAnalyticsResponse> GetSlaAnalyticsAsync(AnalyticsFilter filter, CancellationToken cancellationToken = default)
    {
        var (period, complaints) = await LoadComplaintsAsync(filter, cancellationToken);
        return await BuildSlaAsync(period, complaints, cancellationToken);
    }

    public async Task<SatisfactionAnalyticsResponse> GetSatisfactionAnalyticsAsync(AnalyticsFilter filter, CancellationToken cancellationToken = default)
    {
        var (period, complaints) = await LoadComplaintsAsync(filter, cancellationToken);
        return await BuildSatisfactionAsync(period, complaints, cancellationToken);
    }

    public async Task<IReadOnlyList<HeatmapPointResponse>> GetHeatmapAsync(AnalyticsFilter filter, CancellationToken cancellationToken = default)
    {
        var (_, complaints) = await LoadComplaintsAsync(filter, cancellationToken);
        return complaints
            .Where(c => c.Latitude is >= -90 and <= 90 && c.Longitude is >= -180 and <= 180)
            .GroupBy(c => new
            {
                Latitude = Math.Round(c.Latitude, 3),
                Longitude = Math.Round(c.Longitude, 3),
                c.WardId,
                WardName = c.Ward?.Name
            })
            .Select(group =>
            {
                var items = group.ToList();
                var high = items.Count(c => c.Priority == ComplaintPriority.High);
                var critical = items.Count(c => c.Priority == ComplaintPriority.Critical);
                var active = items.Count(c => !TerminalStatuses.Contains(c.Status));
                var resolutionPending = items.Count(c => c.Status is ComplaintStatus.Resolved or ComplaintStatus.VerificationPending);
                var solved = items.Count(c => ClosedStatuses.Contains(c.Status));
                var disputed = items.Count(c => c.Status is ComplaintStatus.Disputed or ComplaintStatus.Appealed);
                var score = Round(active + (high * 2m) + (critical * 3m));
                var dominant = items.GroupBy(c => string.IsNullOrWhiteSpace(c.Category) ? "Uncategorised" : c.Category)
                    .OrderByDescending(g => g.Count()).First().Key;
                var risk = score >= 25 ? "Critical" : score >= 12 ? "High" : score >= 5 ? "Medium" : "Low";
                var resolutionHours = items
                    .Where(c => c.ClosedAt.HasValue || c.ResolvedAt.HasValue)
                    .Select(c => ((c.ClosedAt ?? c.ResolvedAt)!.Value - c.CreatedAt).TotalHours)
                    .Where(value => value >= 0)
                    .ToList();
                var latest = items.OrderByDescending(c => c.UpdatedAt).ThenByDescending(c => c.Id).First();

                return new HeatmapPointResponse(
                    group.Key.Latitude,
                    group.Key.Longitude,
                    items.Count,
                    active,
                    resolutionPending,
                    solved,
                    disputed,
                    Percent(solved, items.Count),
                    Round(resolutionHours.Count == 0 ? 0 : (decimal)resolutionHours.Average()),
                    score,
                    dominant,
                    risk,
                    group.Key.WardId,
                    group.Key.WardName,
                    latest.Id,
                    latest.Title,
                    latest.Status.ToString(),
                    latest.UpdatedAt,
                    Slice(items.GroupBy(c => string.IsNullOrWhiteSpace(c.Category) ? "Uncategorised" : c.Category).Select(g => (g.Key, g.Count())), items.Count),
                    Slice(items.GroupBy(c => c.Status.ToString()).Select(g => (g.Key, g.Count())), items.Count));
            })
            .OrderByDescending(point => point.HeatScore)
            .ThenByDescending(point => point.ComplaintCount)
            .Take(500)
            .ToList();
    }

    public async Task<IReadOnlyList<PublicHeatmapPointResponse>> GetPublicHeatmapAsync(AnalyticsFilter filter, CancellationToken cancellationToken = default)
    {
        var period = ResolvePublicPeriod(filter);
        var query = _db.Complaints.AsNoTracking()
            .Include(c => c.Ward)
            .Where(c => !c.IsDeleted &&
                        c.CreatedAt >= period.From && c.CreatedAt <= period.To &&
                        PublicHeatmapStatuses.Contains(c.Status) &&
                        c.Latitude >= -90 && c.Latitude <= 90 &&
                        c.Longitude >= -180 && c.Longitude <= 180);

        if (filter.DepartmentId.HasValue)
            query = query.Where(c => c.DepartmentId == filter.DepartmentId.Value);
        if (filter.WardId.HasValue)
            query = query.Where(c => c.WardId == filter.WardId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Category) && !filter.Category.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            var category = filter.Category.Trim();
            query = query.Where(c => c.Category == category);
        }
        if (!string.IsNullOrWhiteSpace(filter.Status) && !filter.Status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            if (!Enum.TryParse<ComplaintStatus>(filter.Status.Trim(), true, out var status))
                throw new CivicHero.Backend.Core.Exceptions.ValidationException([$"Unknown complaint status '{filter.Status}'."]);
            if (!PublicHeatmapStatuses.Contains(status))
                throw new CivicHero.Backend.Core.Exceptions.ValidationException(["The requested status is not available on the public heatmap."]);
            query = query.Where(c => c.Status == status);
        }

        var complaints = await query
            .OrderByDescending(c => c.CreatedAt)
            .Take(25000)
            .ToListAsync(cancellationToken);
        return complaints
            // Public coordinates are deliberately reduced to roughly one-kilometre cells.
            // The public response never includes a complaint ID, title, address, citizen,
            // evidence, or exact latitude/longitude.
            .GroupBy(c => new
            {
                Latitude = Math.Round(c.Latitude, 2),
                Longitude = Math.Round(c.Longitude, 2),
                c.WardId,
                WardName = c.Ward?.Name
            })
            .Select(group =>
            {
                var items = group.ToList();
                var high = items.Count(c => c.Priority == ComplaintPriority.High);
                var critical = items.Count(c => c.Priority == ComplaintPriority.Critical);
                var active = items.Count(c => !ClosedStatuses.Contains(c.Status));
                var resolutionPending = items.Count(c => c.Status is ComplaintStatus.Resolved or ComplaintStatus.VerificationPending);
                var solved = items.Count(c => ClosedStatuses.Contains(c.Status));
                var disputed = items.Count(c => c.Status is ComplaintStatus.Disputed or ComplaintStatus.Appealed);
                var score = Round(active + (high * 2m) + (critical * 3m));
                var dominant = items.GroupBy(c => string.IsNullOrWhiteSpace(c.Category) ? "Uncategorised" : c.Category)
                    .OrderByDescending(g => g.Count()).ThenBy(g => g.Key).First().Key;
                var risk = score >= 25 ? "Critical" : score >= 12 ? "High" : score >= 5 ? "Medium" : "Low";
                var resolutionHours = items
                    .Where(c => c.ClosedAt.HasValue || c.ResolvedAt.HasValue)
                    .Select(c => ((c.ClosedAt ?? c.ResolvedAt)!.Value - c.CreatedAt).TotalHours)
                    .Where(value => value >= 0)
                    .ToList();

                return new PublicHeatmapPointResponse(
                    group.Key.Latitude,
                    group.Key.Longitude,
                    items.Count,
                    active,
                    resolutionPending,
                    solved,
                    disputed,
                    Percent(solved, items.Count),
                    Round(resolutionHours.Count == 0 ? 0 : (decimal)resolutionHours.Average()),
                    score,
                    dominant,
                    risk,
                    group.Key.WardId,
                    group.Key.WardName,
                    items.Max(c => c.UpdatedAt),
                    Slice(items.GroupBy(c => string.IsNullOrWhiteSpace(c.Category) ? "Uncategorised" : c.Category).Select(g => (g.Key, g.Count())), items.Count),
                    Slice(items.GroupBy(c => c.Status.ToString()).Select(g => (g.Key, g.Count())), items.Count));
            })
            .OrderByDescending(point => point.HeatScore)
            .ThenByDescending(point => point.ComplaintCount)
            .Take(200)
            .ToList();
    }

    public Task<AnalyticsExportResult> ExportAsync(string report, AnalyticsFilter filter, CancellationToken cancellationToken = default) =>
        ExportAsync(report, "csv", filter, cancellationToken);

    public async Task<AnalyticsExportResult> ExportAsync(string report, string format, AnalyticsFilter filter, CancellationToken cancellationToken = default)
    {
        report = (report ?? string.Empty).Trim().ToLowerInvariant();
        var generatedAt = DateTimeOffset.UtcNow;
        ReportTable table;

        switch (report)
        {
            case "overview":
            {
                var row = await GetOverviewAsync(filter, cancellationToken);
                table = new ReportTable(
                    "CivicHero analytics overview",
                    ["Metric", "Value"],
                    [
                        ["Period from", row.Period.From], ["Period to", row.Period.To],
                        ["Total complaints", row.TotalComplaints], ["Open complaints", row.OpenComplaints],
                        ["Closed complaints", row.ClosedComplaints], ["Resolution rate (%)", row.ResolutionRate],
                        ["Average resolution hours", row.AverageResolutionHours], ["SLA compliance (%)", row.SlaCompliance],
                        ["Satisfaction score", row.SatisfactionScore], ["Registered users", row.RegisteredUsers],
                        ["Active users", row.ActiveUsers], ["Active departments", row.ActiveDepartments],
                        ["Active wards", row.ActiveWards]
                    ],
                    $"Generated {generatedAt:yyyy-MM-dd HH:mm} UTC");
                break;
            }
            case "complaints":
            {
                var (period, complaints) = await LoadComplaintsAsync(filter, cancellationToken);
                table = new ReportTable(
                    "CivicHero complaint register",
                    ["Complaint ID", "Created at", "Title", "Category", "Priority", "Status", "Department", "Ward", "Officer", "Resolved at", "Closed at"],
                    complaints.OrderByDescending(item => item.CreatedAt).Take(5000).Select(item => (IReadOnlyList<object?>)new object?[]
                    { item.Id, item.CreatedAt, item.Title, item.Category, item.Priority.ToString(), item.Status.ToString(), item.Department?.Name, item.Ward?.Name, item.AssignedOfficer?.FullName, item.ResolvedAt, item.ClosedAt }).ToList(),
                    $"Period {period.From:yyyy-MM-dd} to {period.To:yyyy-MM-dd}");
                break;
            }
            case "departments":
            {
                var rows = await GetDepartmentAnalyticsAsync(filter, cancellationToken);
                table = new ReportTable(
                    "CivicHero department performance",
                    ["Rank", "Department", "Total", "Open", "Closed", "Escalated", "Resolution rate (%)", "Average resolution hours", "SLA compliance (%)", "Satisfaction"],
                    rows.Select(row => (IReadOnlyList<object?>)new object?[] { row.Rank, row.DepartmentName, row.Total, row.Open, row.Closed, row.Escalated, row.ResolutionRate, row.AverageResolutionHours, row.SlaCompliance, row.SatisfactionScore }).ToList(),
                    $"Generated {generatedAt:yyyy-MM-dd HH:mm} UTC");
                break;
            }
            case "officers":
            {
                var rows = await GetOfficerAnalyticsAsync(filter, cancellationToken);
                table = new ReportTable(
                    "CivicHero Officer performance",
                    ["Officer", "Department", "Ward", "Assigned", "Active", "Completed", "Completion rate (%)", "Average completion hours", "SLA compliance (%)", "Revisit rate (%)"],
                    rows.Select(row => (IReadOnlyList<object?>)new object?[] { row.OfficerName, row.DepartmentName, row.WardName, row.Assigned, row.Active, row.Completed, row.CompletionRate, row.AverageCompletionHours, row.SlaCompliance, row.RevisitRate }).ToList(),
                    $"Generated {generatedAt:yyyy-MM-dd HH:mm} UTC");
                break;
            }
            case "wards":
            {
                var rows = await GetWardAnalyticsAsync(filter, cancellationToken);
                table = new ReportTable(
                    "CivicHero ward analysis",
                    ["Ward", "Department", "Total", "Open", "High priority", "Critical", "Closed", "Resolution rate (%)", "Heat score", "Latitude", "Longitude"],
                    rows.Select(row => (IReadOnlyList<object?>)new object?[] { row.WardName, row.DepartmentName, row.Total, row.Open, row.HighPriority, row.Critical, row.Closed, row.ResolutionRate, row.HeatScore, row.Latitude, row.Longitude }).ToList(),
                    $"Generated {generatedAt:yyyy-MM-dd HH:mm} UTC");
                break;
            }
            case "sla":
            {
                var row = await GetSlaAnalyticsAsync(filter, cancellationToken);
                table = new ReportTable(
                    "CivicHero SLA compliance",
                    ["Metric", "Value"],
                    [
                        ["Period from", row.Period.From], ["Period to", row.Period.To],
                        ["Total assignments", row.TotalAssignments], ["Assignment compliant", row.AssignmentCompliant],
                        ["Resolution compliant", row.ResolutionCompliant], ["Currently overdue", row.CurrentlyOverdue],
                        ["Assignment compliance rate (%)", row.AssignmentComplianceRate],
                        ["Resolution compliance rate (%)", row.ResolutionComplianceRate],
                        ["Overall compliance rate (%)", row.OverallComplianceRate]
                    ],
                    $"Generated {generatedAt:yyyy-MM-dd HH:mm} UTC");
                break;
            }
            case "satisfaction":
            {
                var row = await GetSatisfactionAnalyticsAsync(filter, cancellationToken);
                var satisfactionRows = new List<IReadOnlyList<object?>>
                {
                    new object?[] { "Period from", row.Period.From },
                    new object?[] { "Period to", row.Period.To },
                    new object?[] { "Rating count", row.RatingCount },
                    new object?[] { "Average rating", row.AverageRating },
                    new object?[] { "Approval rate (%)", row.ApprovalRate },
                    new object?[] { "Dispute rate (%)", row.DisputeRate }
                };
                satisfactionRows.AddRange(row.RatingDistribution.Select(item => (IReadOnlyList<object?>)new object?[] { item.Name, item.Count }));
                table = new ReportTable(
                    "CivicHero Citizen satisfaction",
                    ["Metric", "Value"],
                    satisfactionRows,
                    $"Generated {generatedAt:yyyy-MM-dd HH:mm} UTC");
                break;
            }
            case "citizen-engagement":
            case "citizenengagement":
            {
                var (period, rows) = await BuildCitizenEngagementAsync(filter, cancellationToken);
                table = new ReportTable(
                    "CivicHero Citizen engagement",
                    [
                        "Citizen ID", "Citizen", "Email", "Ward", "Account active", "Last login",
                        "Complaints submitted", "Active complaints", "Closed complaints", "Supports cast",
                        "Public comments", "Verification responses", "Approved verifications", "Average service rating",
                        "Reward transactions", "Points earned", "Points deducted", "Reward redemptions", "Points spent",
                        "Initiative follows", "Initiative feedback", "Average initiative rating", "Total engagement actions",
                        "Last activity"
                    ],
                    rows.Select(row => (IReadOnlyList<object?>)new object?[]
                    {
                        row.CitizenId, row.CitizenName, row.CitizenEmail, row.WardName, row.AccountActive, row.LastLoginAt,
                        row.ComplaintsSubmitted, row.ActiveComplaints, row.ClosedComplaints, row.SupportsCast,
                        row.PublicComments, row.VerificationResponses, row.ApprovedVerifications, row.AverageServiceRating,
                        row.RewardTransactions, row.PointsEarned, row.PointsDeducted, row.RewardRedemptions, row.RedemptionPointsSpent,
                        row.InitiativeFollows, row.InitiativeFeedback, row.AverageInitiativeRating, row.TotalEngagementActions,
                        row.LastActivityAt
                    }).ToList(),
                    $"Activity period {period.From:yyyy-MM-dd} to {period.To:yyyy-MM-dd}; {rows.Count} Citizen accounts included (maximum 5,000). Complaint filters apply to complaint-linked engagement.");
                break;
            }
            default:
                throw new ArgumentException("Supported reports: overview, complaints, departments, officers, wards, sla, satisfaction, citizen-engagement.", nameof(report));
        }

        return ReportDocumentBuilder.Build(table, format, $"civichero-{report}-{generatedAt:yyyyMMdd-HHmm}");
    }

    private async Task<(AnalyticsPeriod Period, IReadOnlyList<CitizenEngagementRow> Rows)> BuildCitizenEngagementAsync(
        AnalyticsFilter filter,
        CancellationToken cancellationToken)
    {
        var period = ResolvePeriod(filter);
        var citizensQuery = _db.Users.AsNoTracking()
            .Where(user => !user.IsDeleted && user.Role == UserRole.Citizen);

        if (filter.WardId.HasValue)
            citizensQuery = citizensQuery.Where(user => user.WardId == filter.WardId.Value);

        var citizens = await citizensQuery
            .OrderBy(user => user.FullName)
            .ThenBy(user => user.Id)
            .Select(user => new CitizenAccountRow(
                user.Id,
                user.FullName,
                user.Email,
                user.Ward != null ? user.Ward.Name : null,
                user.IsActive,
                user.LastLoginAt))
            .Take(5000)
            .ToListAsync(cancellationToken);

        if (citizens.Count == 0)
            return (period, Array.Empty<CitizenEngagementRow>());

        var scopedComplaintQuery = _db.Complaints.AsNoTracking().Where(complaint => !complaint.IsDeleted);
        if (filter.DepartmentId.HasValue)
            scopedComplaintQuery = scopedComplaintQuery.Where(complaint => complaint.DepartmentId == filter.DepartmentId.Value);
        if (filter.WardId.HasValue)
            scopedComplaintQuery = scopedComplaintQuery.Where(complaint => complaint.WardId == filter.WardId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Category) && !filter.Category.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            var category = filter.Category.Trim();
            scopedComplaintQuery = scopedComplaintQuery.Where(complaint => complaint.Category == category);
        }
        if (!string.IsNullOrWhiteSpace(filter.Status) && !filter.Status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            if (!Enum.TryParse<ComplaintStatus>(filter.Status.Trim(), true, out var status))
                throw new ArgumentException($"Unknown complaint status '{filter.Status}'.", nameof(filter));
            scopedComplaintQuery = scopedComplaintQuery.Where(complaint => complaint.Status == status);
        }

        var scopedComplaintIds = scopedComplaintQuery.Select(complaint => complaint.Id);

        var complaintEvents = await scopedComplaintQuery
            .Where(complaint => complaint.CreatedAt >= period.From && complaint.CreatedAt <= period.To)
            .Select(complaint => new { complaint.CitizenId, complaint.Status, complaint.CreatedAt })
            .ToListAsync(cancellationToken);

        var voteEvents = await _db.ComplaintVotes.AsNoTracking()
            .Where(vote => vote.VotedAt >= period.From && vote.VotedAt <= period.To && scopedComplaintIds.Contains(vote.ComplaintId))
            .Select(vote => new { vote.UserId, vote.VotedAt })
            .ToListAsync(cancellationToken);

        var commentEvents = await _db.ComplaintComments.AsNoTracking()
            .Where(comment => !comment.IsDeleted &&
                              comment.CreatedAt >= period.From && comment.CreatedAt <= period.To &&
                              comment.Visibility == CommentVisibility.Public &&
                              comment.ModerationStatus == CommentModerationStatus.Visible &&
                              scopedComplaintIds.Contains(comment.ComplaintId))
            .Select(comment => new { comment.UserId, comment.CreatedAt })
            .ToListAsync(cancellationToken);

        var verificationEvents = await _db.ComplaintVerifications.AsNoTracking()
            .Where(verification => verification.CompletedAt.HasValue &&
                                   verification.CompletedAt.Value >= period.From && verification.CompletedAt.Value <= period.To &&
                                   verification.Decision != VerificationDecision.Pending &&
                                   scopedComplaintIds.Contains(verification.ComplaintId))
            .Select(verification => new
            {
                verification.CitizenId,
                verification.Decision,
                verification.Rating,
                CompletedAt = verification.CompletedAt!.Value
            })
            .ToListAsync(cancellationToken);

        var rewardEvents = await _db.ReputationLogs.AsNoTracking()
            .Where(log => log.CreatedAt >= period.From && log.CreatedAt <= period.To)
            .Select(log => new { log.UserId, log.PointsDelta, log.CreatedAt })
            .ToListAsync(cancellationToken);

        var redemptionEvents = await _db.Redemptions.AsNoTracking()
            .Where(redemption => redemption.CreatedAt >= period.From && redemption.CreatedAt <= period.To)
            .Select(redemption => new { redemption.UserId, redemption.PointsSpent, redemption.CreatedAt })
            .ToListAsync(cancellationToken);

        var initiativeEvents = await _db.AuditLogs.AsNoTracking()
            .Where(log => log.UserId.HasValue &&
                          log.EntityName == "CivicInitiative" &&
                          (log.Action == "InitiativeFollowed" || log.Action == "InitiativeFeedbackSubmitted") &&
                          log.CreatedAt >= period.From && log.CreatedAt <= period.To)
            .Select(log => new { UserId = log.UserId!.Value, log.Action, log.NewValuesJson, log.CreatedAt })
            .ToListAsync(cancellationToken);

        var complaintStats = complaintEvents.GroupBy(item => item.CitizenId).ToDictionary(
            group => group.Key,
            group => new
            {
                Submitted = group.Count(),
                Active = group.Count(item => !TerminalStatuses.Contains(item.Status)),
                Closed = group.Count(item => ClosedStatuses.Contains(item.Status)),
                LastActivity = group.Max(item => (DateTimeOffset?)item.CreatedAt)
            });

        var voteStats = voteEvents.GroupBy(item => item.UserId).ToDictionary(
            group => group.Key,
            group => new { Count = group.Count(), LastActivity = group.Max(item => (DateTimeOffset?)item.VotedAt) });

        var commentStats = commentEvents.GroupBy(item => item.UserId).ToDictionary(
            group => group.Key,
            group => new { Count = group.Count(), LastActivity = group.Max(item => (DateTimeOffset?)item.CreatedAt) });

        var verificationStats = verificationEvents.GroupBy(item => item.CitizenId).ToDictionary(
            group => group.Key,
            group =>
            {
                var ratings = group.Where(item => item.Rating is >= 1 and <= 5).Select(item => item.Rating!.Value).ToList();
                return new
                {
                    Count = group.Count(),
                    Approved = group.Count(item => item.Decision == VerificationDecision.Approved),
                    AverageRating = Round(ratings.Count == 0 ? 0 : (decimal)ratings.Average()),
                    LastActivity = group.Max(item => (DateTimeOffset?)item.CompletedAt)
                };
            });

        var rewardStats = rewardEvents.GroupBy(item => item.UserId).ToDictionary(
            group => group.Key,
            group => new
            {
                Count = group.Count(),
                Earned = group.Where(item => item.PointsDelta > 0).Sum(item => item.PointsDelta),
                Deducted = group.Where(item => item.PointsDelta < 0).Sum(item => -item.PointsDelta),
                LastActivity = group.Max(item => (DateTimeOffset?)item.CreatedAt)
            });

        var redemptionStats = redemptionEvents.GroupBy(item => item.UserId).ToDictionary(
            group => group.Key,
            group => new
            {
                Count = group.Count(),
                PointsSpent = group.Sum(item => item.PointsSpent),
                LastActivity = group.Max(item => (DateTimeOffset?)item.CreatedAt)
            });

        var initiativeStats = initiativeEvents.GroupBy(item => item.UserId).ToDictionary(
            group => group.Key,
            group =>
            {
                var ratings = group
                    .Where(item => item.Action == "InitiativeFeedbackSubmitted")
                    .Select(item => ReadJsonInt(item.NewValuesJson, "rating"))
                    .Where(value => value is >= 1 and <= 5)
                    .Select(value => value!.Value)
                    .ToList();
                return new
                {
                    Follows = group.Count(item => item.Action == "InitiativeFollowed"),
                    Feedback = group.Count(item => item.Action == "InitiativeFeedbackSubmitted"),
                    AverageRating = Round(ratings.Count == 0 ? 0 : (decimal)ratings.Average()),
                    LastActivity = group.Max(item => (DateTimeOffset?)item.CreatedAt)
                };
            });

        var rows = citizens.Select(citizen =>
        {
            complaintStats.TryGetValue(citizen.Id, out var complaints);
            voteStats.TryGetValue(citizen.Id, out var votes);
            commentStats.TryGetValue(citizen.Id, out var comments);
            verificationStats.TryGetValue(citizen.Id, out var verifications);
            rewardStats.TryGetValue(citizen.Id, out var rewards);
            redemptionStats.TryGetValue(citizen.Id, out var redemptions);
            initiativeStats.TryGetValue(citizen.Id, out var initiatives);

            var totalActions = (complaints?.Submitted ?? 0) +
                               (votes?.Count ?? 0) +
                               (comments?.Count ?? 0) +
                               (verifications?.Count ?? 0) +
                               (redemptions?.Count ?? 0) +
                               (initiatives?.Follows ?? 0) +
                               (initiatives?.Feedback ?? 0);

            return new CitizenEngagementRow(
                citizen.Id,
                citizen.FullName,
                citizen.Email,
                citizen.WardName,
                citizen.IsActive,
                citizen.LastLoginAt,
                complaints?.Submitted ?? 0,
                complaints?.Active ?? 0,
                complaints?.Closed ?? 0,
                votes?.Count ?? 0,
                comments?.Count ?? 0,
                verifications?.Count ?? 0,
                verifications?.Approved ?? 0,
                verifications?.AverageRating ?? 0,
                rewards?.Count ?? 0,
                rewards?.Earned ?? 0,
                rewards?.Deducted ?? 0,
                redemptions?.Count ?? 0,
                redemptions?.PointsSpent ?? 0,
                initiatives?.Follows ?? 0,
                initiatives?.Feedback ?? 0,
                initiatives?.AverageRating ?? 0,
                totalActions,
                LatestActivity(
                    complaints?.LastActivity,
                    votes?.LastActivity,
                    comments?.LastActivity,
                    verifications?.LastActivity,
                    rewards?.LastActivity,
                    redemptions?.LastActivity,
                    initiatives?.LastActivity));
        })
        .OrderByDescending(row => row.TotalEngagementActions)
        .ThenByDescending(row => row.LastActivityAt)
        .ThenBy(row => row.CitizenName)
        .ToList();

        return (period, rows);
    }

    private static DateTimeOffset? LatestActivity(params DateTimeOffset?[] values)
    {
        var available = values.Where(value => value.HasValue).Select(value => value!.Value).ToArray();
        return available.Length == 0 ? null : available.Max();
    }

    private static int? ReadJsonInt(string? json, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var result)
                ? result
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<(AnalyticsPeriod Period, List<Complaint> Complaints)> LoadComplaintsAsync(AnalyticsFilter filter, CancellationToken cancellationToken)
    {
        var period = ResolvePeriod(filter);
        var query = _db.Complaints.AsNoTracking()
            .Include(c => c.Department)
            .Include(c => c.Ward)
            .Include(c => c.AssignedOfficer)
            .Where(c => !c.IsDeleted && c.CreatedAt >= period.From && c.CreatedAt <= period.To);

        var role = _currentUser.Role ?? string.Empty;
        if (role.Equals("Supervisor", StringComparison.OrdinalIgnoreCase))
        {
            if (!_currentUser.DepartmentId.HasValue)
                return (period, []);
            var scopedDepartment = _currentUser.DepartmentId.Value;
            query = query.Where(c => c.DepartmentId == scopedDepartment);
        }

        if (filter.DepartmentId.HasValue)
            query = query.Where(c => c.DepartmentId == filter.DepartmentId.Value);
        if (filter.WardId.HasValue)
            query = query.Where(c => c.WardId == filter.WardId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Category) && !filter.Category.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            var category = filter.Category.Trim();
            query = query.Where(c => c.Category == category);
        }
        if (!string.IsNullOrWhiteSpace(filter.Status) && !filter.Status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            if (!Enum.TryParse<ComplaintStatus>(filter.Status.Trim(), true, out var status))
                throw new ArgumentException($"Unknown complaint status '{filter.Status}'.", nameof(filter));
            query = query.Where(c => c.Status == status);
        }

        return (period, await query.ToListAsync(cancellationToken));
    }

    private async Task<List<ComplaintAssignment>> LoadAssignmentsAsync(IReadOnlyCollection<Complaint> complaints, CancellationToken cancellationToken)
    {
        var ids = complaints.Select(c => c.Id).ToHashSet();
        if (ids.Count == 0) return [];
        return await _db.ComplaintAssignments.AsNoTracking().Where(a => ids.Contains(a.ComplaintId)).ToListAsync(cancellationToken);
    }

    private async Task<SlaAnalyticsResponse> BuildSlaAsync(AnalyticsPeriod period, IReadOnlyCollection<Complaint> complaints, CancellationToken cancellationToken)
    {
        var assignments = await LoadAssignmentsAsync(complaints, cancellationToken);
        var assignmentCompleted = assignments.Where(a => a.RespondedAt.HasValue).ToList();
        var resolutionCompleted = assignments.Where(a => a.CompletedAt.HasValue).ToList();
        var now = DateTimeOffset.UtcNow;
        var overdue = assignments.Count(a => a.IsCurrent && !a.CompletedAt.HasValue && a.ResolutionDueAt < now);
        var byPriority = complaints.GroupBy(c => c.Priority.ToString()).Select(group =>
        {
            var ids = group.Select(c => c.Id).ToHashSet();
            var related = assignments.Where(a => ids.Contains(a.ComplaintId) && a.CompletedAt.HasValue).ToList();
            var compliant = related.Count(a => a.CompletedAt!.Value <= a.ResolutionDueAt);
            return (group.Key, compliant);
        });
        var totalResolution = resolutionCompleted.Count;
        var compliantResolution = resolutionCompleted.Count(a => a.CompletedAt!.Value <= a.ResolutionDueAt);
        var assignmentCompliant = assignmentCompleted.Count(a => a.RespondedAt!.Value <= a.AssignmentDueAt);
        var totalCheckpoints = assignmentCompleted.Count + totalResolution;
        var compliantCheckpoints = assignmentCompliant + compliantResolution;
        return new SlaAnalyticsResponse(period, assignments.Count, assignmentCompliant, compliantResolution, overdue,
            Percent(assignmentCompliant, assignmentCompleted.Count), Percent(compliantResolution, totalResolution), Percent(compliantCheckpoints, totalCheckpoints),
            Slice(byPriority, Math.Max(1, compliantResolution)));
    }

    private async Task<SatisfactionAnalyticsResponse> BuildSatisfactionAsync(AnalyticsPeriod period, IReadOnlyCollection<Complaint> complaints, CancellationToken cancellationToken)
    {
        var ratings = await LoadRatingsAsync(complaints, cancellationToken);
        var values = ratings.Select(r => r.Rating).Where(r => r is >= 1 and <= 5).ToList();
        var disputed = complaints.Count(c => c.Status == ComplaintStatus.Disputed || c.Status == ComplaintStatus.Appealed);
        var approved = ratings.Count(r => r.IsApproved == true);
        var decisions = ratings.Count(r => r.IsApproved.HasValue);
        var distribution = Enumerable.Range(1, 5).Select(star => ($"{star} star", values.Count(value => value == star)));
        return new SatisfactionAnalyticsResponse(period, values.Count, Round(values.Count == 0 ? 0 : (decimal)values.Average()),
            Percent(approved, decisions), Percent(disputed, complaints.Count), Slice(distribution, Math.Max(1, values.Count)));
    }

    private async Task<List<RatingRow>> LoadRatingsAsync(IReadOnlyCollection<Complaint> complaints, CancellationToken cancellationToken)
    {
        var ids = complaints.Select(c => c.Id).ToHashSet();
        if (ids.Count == 0) return [];
        var rows = await _db.ComplaintVerifications.AsNoTracking().ToListAsync(cancellationToken);
        return rows.Select(row => new RatingRow(
                ReadLong(row, "ComplaintId"),
                ReadInt(row, "Rating", "CitizenRating", "ServiceRating"),
                ReadBoolean(row, "IsApproved", "Approved") ?? ReadDecision(row)))
            .Where(row => ids.Contains(row.ComplaintId))
            .ToList();
    }

    private static bool? ReadDecision(object row)
    {
        var value = ReadString(row, "Decision", "Status", "Result", "VerificationStatus");
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (value.Contains("approve", StringComparison.OrdinalIgnoreCase) || value.Contains("accept", StringComparison.OrdinalIgnoreCase)) return true;
        if (value.Contains("reject", StringComparison.OrdinalIgnoreCase) || value.Contains("dispute", StringComparison.OrdinalIgnoreCase)) return false;
        return null;
    }

    private static AnalyticsPeriod ResolvePeriod(AnalyticsFilter filter)
    {
        var to = filter.To?.ToUniversalTime() ?? DateTimeOffset.UtcNow;
        var from = filter.From?.ToUniversalTime() ?? to.AddDays(-89);
        if (from > to) throw new ArgumentException("The analytics From date must be before To date.");
        if ((to - from).TotalDays > 730) from = to.AddDays(-730);
        return new AnalyticsPeriod(from, to, Math.Max(1, (int)Math.Ceiling((to - from).TotalDays)));
    }

    private static AnalyticsPeriod ResolvePublicPeriod(AnalyticsFilter filter)
    {
        var now = DateTimeOffset.UtcNow;
        var to = filter.To?.ToUniversalTime() ?? now;
        if (to > now.AddMinutes(5)) to = now;
        var from = filter.From?.ToUniversalTime() ?? to.AddDays(-29);
        if (from > to) throw new CivicHero.Backend.Core.Exceptions.ValidationException(["The public heatmap From date must be before To date."]);
        if ((to - from).TotalDays > 365) from = to.AddDays(-365);
        return new AnalyticsPeriod(from, to, Math.Max(1, (int)Math.Ceiling((to - from).TotalDays)));
    }

    private static IReadOnlyList<TrendPoint> BuildTrend(AnalyticsPeriod period, IReadOnlyCollection<Complaint> complaints)
    {
        var start = new DateTimeOffset(period.From.Year, period.From.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(period.To.Year, period.To.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var result = new List<TrendPoint>();
        for (var month = start; month <= end; month = month.AddMonths(1))
        {
            var next = month.AddMonths(1);
            result.Add(new TrendPoint(month.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                complaints.Count(c => c.CreatedAt >= month && c.CreatedAt < next),
                complaints.Count(c => c.ClosedAt >= month && c.ClosedAt < next),
                complaints.Count(c => (c.Status == ComplaintStatus.Escalated || c.Status == ComplaintStatus.Appealed) && c.UpdatedAt >= month && c.UpdatedAt < next)));
        }
        return result;
    }

    private static IReadOnlyList<MetricSlice> Slice(IEnumerable<(string Name, int Count)> source, int total) => source
        .OrderByDescending(item => item.Count)
        .ThenBy(item => item.Name)
        .Select(item => new MetricSlice(item.Name, item.Count, Percent(item.Count, total)))
        .ToList();

    private static decimal Percent(int numerator, int denominator) => denominator <= 0 ? 0 : Round(numerator * 100m / denominator);
    private static decimal Round(decimal value, int digits = 2) => Math.Round(value, digits, MidpointRounding.AwayFromZero);

    private static long ReadLong(object target, params string[] names) => Convert.ToInt64(ReadProperty(target, names) ?? 0, CultureInfo.InvariantCulture);
    private static int ReadInt(object target, params string[] names) => Convert.ToInt32(ReadProperty(target, names) ?? 0, CultureInfo.InvariantCulture);
    private static bool? ReadBoolean(object target, params string[] names)
    {
        var value = ReadProperty(target, names);
        return value is null ? null : Convert.ToBoolean(value, CultureInfo.InvariantCulture);
    }
    private static string? ReadString(object target, params string[] names) => ReadProperty(target, names)?.ToString();
    private static object? ReadProperty(object target, params string[] names)
    {
        var type = target.GetType();
        foreach (var name in names)
        {
            var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property is not null) return property.GetValue(target);
        }
        return null;
    }

    private static void CsvRow(StringBuilder builder, params object?[] values)
    {
        builder.AppendLine(string.Join(',', values.Select(CsvValue)));
    }

    private static string CsvValue(object? value)
    {
        if (value is null) return string.Empty;
        var text = value switch
        {
            DateTimeOffset date => date.ToString("O", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
        return $"\"{text.Replace("\"", "\"\"")}\"";
    }

    private sealed record CitizenAccountRow(
        long Id,
        string FullName,
        string Email,
        string? WardName,
        bool IsActive,
        DateTimeOffset? LastLoginAt);

    private sealed record RatingRow(long ComplaintId, int Rating, bool? IsApproved);
}
