using System.Globalization;
using System.Reflection;
using System.Text;
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

    public async Task<AnalyticsExportResult> ExportAsync(string report, AnalyticsFilter filter, CancellationToken cancellationToken = default)
    {
        report = (report ?? string.Empty).Trim().ToLowerInvariant();
        var builder = new StringBuilder();
        switch (report)
        {
            case "complaints":
            {
                var (_, complaints) = await LoadComplaintsAsync(filter, cancellationToken);
                CsvRow(builder, "ComplaintId", "CreatedAt", "Title", "Category", "Priority", "Status", "Department", "Ward", "Officer", "ResolvedAt", "ClosedAt");
                foreach (var item in complaints.OrderByDescending(c => c.CreatedAt))
                    CsvRow(builder, item.Id, item.CreatedAt, item.Title, item.Category, item.Priority, item.Status, item.Department?.Name, item.Ward?.Name, item.AssignedOfficer?.FullName, item.ResolvedAt, item.ClosedAt);
                break;
            }
            case "departments":
                CsvRow(builder, "Rank", "Department", "Total", "Open", "Closed", "Escalated", "ResolutionRate", "AverageResolutionHours", "SlaCompliance", "Satisfaction");
                foreach (var row in await GetDepartmentAnalyticsAsync(filter, cancellationToken)) CsvRow(builder, row.Rank, row.DepartmentName, row.Total, row.Open, row.Closed, row.Escalated, row.ResolutionRate, row.AverageResolutionHours, row.SlaCompliance, row.SatisfactionScore);
                break;
            case "officers":
                CsvRow(builder, "Officer", "Department", "Ward", "Assigned", "Active", "Completed", "CompletionRate", "AverageCompletionHours", "SlaCompliance", "RevisitRate");
                foreach (var row in await GetOfficerAnalyticsAsync(filter, cancellationToken)) CsvRow(builder, row.OfficerName, row.DepartmentName, row.WardName, row.Assigned, row.Active, row.Completed, row.CompletionRate, row.AverageCompletionHours, row.SlaCompliance, row.RevisitRate);
                break;
            case "wards":
                CsvRow(builder, "Ward", "Department", "Total", "Open", "HighPriority", "Critical", "Closed", "ResolutionRate", "HeatScore", "Latitude", "Longitude");
                foreach (var row in await GetWardAnalyticsAsync(filter, cancellationToken)) CsvRow(builder, row.WardName, row.DepartmentName, row.Total, row.Open, row.HighPriority, row.Critical, row.Closed, row.ResolutionRate, row.HeatScore, row.Latitude, row.Longitude);
                break;
            case "sla":
            {
                var row = await GetSlaAnalyticsAsync(filter, cancellationToken);
                CsvRow(builder, "Metric", "Value");
                CsvRow(builder, "Total assignments", row.TotalAssignments);
                CsvRow(builder, "Assignment compliant", row.AssignmentCompliant);
                CsvRow(builder, "Resolution compliant", row.ResolutionCompliant);
                CsvRow(builder, "Currently overdue", row.CurrentlyOverdue);
                CsvRow(builder, "Overall compliance rate", row.OverallComplianceRate);
                break;
            }
            default:
                throw new ArgumentException("Supported reports: complaints, departments, officers, wards, sla.", nameof(report));
        }

        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(builder.ToString());
        return new AnalyticsExportResult(bytes, "text/csv; charset=utf-8", $"civichero-{report}-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv");
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

    private sealed record RatingRow(long ComplaintId, int Rating, bool? IsApproved);
}
