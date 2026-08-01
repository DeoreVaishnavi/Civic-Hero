using CivicHero.Backend.Core.DTOs.Disputes;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Core.Services;

public sealed class DisputeService : IDisputeService
{
    private readonly CivicDbContext _db;
    private readonly ICurrentUserService _current;

    public DisputeService(CivicDbContext db, ICurrentUserService current)
    {
        _db = db;
        _current = current;
    }

    public async Task<IReadOnlyList<DisputeResponse>> MineAsync(CancellationToken ct = default)
    {
        EnsureCitizen();
        return (await Query().Where(x => x.RaisedByUserId == RequireUser()).OrderByDescending(x => x.RaisedAt).ToListAsync(ct)).Select(Map).ToArray();
    }

    public async Task<IReadOnlyList<DisputeResponse>> QueueAsync(bool appealsOnly, CancellationToken ct = default)
    {
        EnsureReviewRole();
        var query = ApplyScope(Query());
        query = appealsOnly
            ? query.Where(x => x.Status == DisputeStatus.Appealed || x.Status == DisputeStatus.UnderAdminReview)
            : query.Where(x => x.Status == DisputeStatus.Raised || x.Status == DisputeStatus.UnderSupervisorReview);
        return (await query.OrderBy(x => x.RaisedAt).Take(200).ToListAsync(ct)).Select(Map).ToArray();
    }

    public async Task<DisputeResponse> GetAsync(long id, CancellationToken ct = default)
    {
        var item = await Query().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Dispute was not found.");
        EnsureCanView(item);
        return Map(item);
    }

    public async Task<IReadOnlyList<DisputeHistoryResponse>> HistoryAsync(long id, CancellationToken ct = default)
    {
        var current = await Query().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Dispute was not found.");
        EnsureCanView(current);
        return await ApplyScope(Query()).Where(x => x.ComplaintId == current.ComplaintId).OrderByDescending(x => x.CycleNumber)
            .Select(x => new DisputeHistoryResponse(x.Id, x.ComplaintId, x.CycleNumber, x.Status.ToString(), x.CitizenRemarks,
                x.SupervisorDecision, x.SupervisorRemarks, x.AdminDecision, x.AdminRemarks, x.RaisedAt, x.ReviewedAt, x.ResolvedAt, x.AppealDeadline))
            .ToListAsync(ct);
    }

    public async Task<DisputeResponse> RaiseAsync(long complaintId, RaiseDisputeRequest request, CancellationToken ct = default)
    {
        EnsureCitizen();
        var complaint = await _db.Complaints.Include(x => x.Timeline).SingleOrDefaultAsync(x => x.Id == complaintId && x.CitizenId == RequireUser(), ct)
            ?? throw new NotFoundException("Complaint was not found.");
        if (complaint.Status != ComplaintStatus.VerificationPending && complaint.Status != ComplaintStatus.Resolved)
            throw new BusinessRuleViolationException("Only a resolved complaint awaiting verification can be disputed.");

        var now = DateTimeOffset.UtcNow;
        var item = new DisputeAuditLog
        {
            ComplaintId = complaintId,
            RaisedByUserId = RequireUser(),
            Status = DisputeStatus.UnderSupervisorReview,
            CycleNumber = await _db.DisputeAuditLogs.CountAsync(x => x.ComplaintId == complaintId, ct) + 1,
            CitizenRemarks = request.Reason.Trim(),
            RaisedAt = now
        };
        _db.Add(item);
        complaint.Status = ComplaintStatus.Disputed;
        complaint.Timeline.Add(T(complaint.Id, "DISPUTE_RAISED", "Citizen raised a dispute: " + request.Reason.Trim()));
        await _db.SaveChangesAsync(ct);
        return await GetAsync(item.Id, ct);
    }

    public async Task<DisputeResponse> SupervisorDecisionAsync(long id, DisputeDecisionRequest request, CancellationToken ct = default)
    {
        EnsureSupervisor();
        var item = await Query(true).SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Dispute was not found.");
        EnsureCanView(item);
        if (item.Status != DisputeStatus.UnderSupervisorReview && item.Status != DisputeStatus.Raised)
            throw new BusinessRuleViolationException("This dispute is not awaiting supervisor review.");

        var decision = NormalizeSupervisorDecision(request.Decision);
        var now = DateTimeOffset.UtcNow;
        item.ReviewedByUserId = RequireUser();
        item.ReviewedAt = now;
        item.SupervisorDecision = decision;
        item.SupervisorRemarks = request.Remarks.Trim();

        switch (decision)
        {
            case "CitizenCorrect":
            case "Rework":
            case "Reopen":
                item.Status = DisputeStatus.ReworkOrdered;
                item.ResolvedAt = now;
                item.Complaint.Status = ComplaintStatus.InProgress;
                item.Complaint.ClosedAt = null;
                item.Complaint.ResolvedAt = null;
                item.Complaint.Timeline.Add(T(item.ComplaintId, "REWORK_ORDERED", "Supervisor ordered rework: " + request.Remarks.Trim()));
                break;

            case "AdditionalInvestigation":
                item.Status = DisputeStatus.UnderSupervisorReview;
                item.ResolvedAt = null;
                item.Complaint.Status = ComplaintStatus.Disputed;
                item.Complaint.Timeline.Add(T(item.ComplaintId, "ADDITIONAL_INVESTIGATION_REQUESTED", request.Remarks.Trim()));
                break;

            case "EscalateToSuperAdmin":
                item.Status = DisputeStatus.UnderAdminReview;
                item.ResolvedAt = null;
                item.AdminRemarks = "Escalated by Supervisor: " + request.Remarks.Trim();
                item.Complaint.Status = ComplaintStatus.Appealed;
                item.Complaint.Timeline.Add(T(item.ComplaintId, "DISPUTE_ESCALATED_TO_SUPERADMIN", request.Remarks.Trim()));
                break;

            default:
                item.Status = DisputeStatus.Closed;
                item.ResolvedAt = now;
                item.AppealDeadline = now.AddDays(7);
                item.Complaint.Status = ComplaintStatus.Closed;
                item.Complaint.ClosedAt = now;
                item.Complaint.Timeline.Add(T(item.ComplaintId, "DISPUTE_DECIDED", $"Supervisor {decision}: {request.Remarks.Trim()}"));
                break;
        }

        await _db.SaveChangesAsync(ct);
        return Map(item);
    }

    public async Task<DisputeResponse> AppealAsync(long id, AppealDisputeRequest request, CancellationToken ct = default)
    {
        EnsureCitizen();
        var item = await Query(true).SingleOrDefaultAsync(x => x.Id == id && x.RaisedByUserId == RequireUser(), ct)
            ?? throw new NotFoundException("Dispute was not found.");
        if (item.Status != DisputeStatus.Closed || !item.AppealDeadline.HasValue || item.AppealDeadline < DateTimeOffset.UtcNow)
            throw new BusinessRuleViolationException("The appeal window is closed.");
        item.Status = DisputeStatus.UnderAdminReview;
        item.AdminRemarks = "Citizen appeal: " + request.Remarks.Trim();
        item.Complaint.Status = ComplaintStatus.Appealed;
        item.Complaint.Timeline.Add(T(item.ComplaintId, "DISPUTE_APPEALED", request.Remarks.Trim()));
        await _db.SaveChangesAsync(ct);
        return Map(item);
    }

    public async Task<DisputeResponse> AdminDecisionAsync(long id, DisputeDecisionRequest request, CancellationToken ct = default)
    {
        EnsureAdmin();
        var item = await Query(true).SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Dispute was not found.");
        if (item.Status != DisputeStatus.UnderAdminReview && item.Status != DisputeStatus.Appealed)
            throw new BusinessRuleViolationException("This dispute is not awaiting Admin review.");

        var decision = request.Decision.Trim();
        item.ReviewedByUserId = RequireUser();
        item.AdminDecision = decision;
        item.AdminRemarks = request.Remarks.Trim();
        item.ReviewedAt = DateTimeOffset.UtcNow;
        item.ResolvedAt = DateTimeOffset.UtcNow;
        item.Status = DisputeStatus.Resolved;
        if (decision.Equals("Rework", StringComparison.OrdinalIgnoreCase) || decision.Equals("CitizenCorrect", StringComparison.OrdinalIgnoreCase))
        {
            item.Complaint.Status = ComplaintStatus.InProgress;
            item.Complaint.ClosedAt = null;
            item.Complaint.ResolvedAt = null;
        }
        else if (decision.Equals("Fraud", StringComparison.OrdinalIgnoreCase))
        {
            item.Complaint.Status = ComplaintStatus.ClosedFraud;
            item.Complaint.ClosedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            item.Complaint.Status = ComplaintStatus.Closed;
            item.Complaint.ClosedAt = DateTimeOffset.UtcNow;
        }
        item.Complaint.Timeline.Add(T(item.ComplaintId, "ADMIN_DISPUTE_DECISION", $"Admin {decision}: {request.Remarks.Trim()}"));
        await _db.SaveChangesAsync(ct);
        return Map(item);
    }

    private IQueryable<DisputeAuditLog> Query(bool track = false)
    {
        var query = _db.DisputeAuditLogs.Include(x => x.Complaint).ThenInclude(x => x.Department)
            .Include(x => x.Complaint).ThenInclude(x => x.Ward);
        return track ? query.AsTracking() : query.AsNoTracking();
    }

    private IQueryable<DisputeAuditLog> ApplyScope(IQueryable<DisputeAuditLog> query)
    {
        if (string.Equals(_current.Role, "Supervisor", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => x.Complaint.DepartmentId == _current.DepartmentId);
            if (_current.WardId.HasValue) query = query.Where(x => x.Complaint.WardId == _current.WardId);
        }
        return query;
    }

    private void EnsureCanView(DisputeAuditLog item)
    {
        if (string.Equals(_current.Role, "Citizen", StringComparison.OrdinalIgnoreCase) && item.RaisedByUserId != RequireUser())
            throw new NotFoundException("Dispute was not found.");
        if (string.Equals(_current.Role, "Supervisor", StringComparison.OrdinalIgnoreCase) &&
            (item.Complaint.DepartmentId != _current.DepartmentId || (_current.WardId.HasValue && item.Complaint.WardId != _current.WardId)))
            throw new NotFoundException("Dispute was not found.");
    }

    private static string NormalizeSupervisorDecision(string value)
    {
        var decision = value.Trim();
        if (decision.Equals("OrderRework", StringComparison.OrdinalIgnoreCase)) return "Rework";
        if (decision.Equals("CloseDispute", StringComparison.OrdinalIgnoreCase)) return "Close";
        if (decision.Equals("UpholdResolution", StringComparison.OrdinalIgnoreCase)) return "OfficerEvidenceSufficient";
        return decision;
    }

    private static DisputeResponse Map(DisputeAuditLog item) => new(item.Id, item.ComplaintId,
        "CH-" + item.ComplaintId.ToString("D6"), item.Complaint.Title, item.Status.ToString(), item.CycleNumber,
        item.CitizenRemarks, item.SupervisorDecision, item.SupervisorRemarks, item.AdminDecision, item.AdminRemarks,
        item.RaisedAt, item.AppealDeadline, item.Status == DisputeStatus.Closed && item.AppealDeadline >= DateTimeOffset.UtcNow);

    private ComplaintTimeline T(long complaintId, string eventType, string description) => new()
    {
        ComplaintId = complaintId,
        UserId = RequireUser(),
        EventType = eventType,
        Description = description,
        Timestamp = DateTimeOffset.UtcNow
    };

    private long RequireUser() => _current.UserId ?? throw new BusinessRuleViolationException("Authenticated user is required.");
    private void EnsureCitizen() { if (!string.Equals(_current.Role, "Citizen", StringComparison.OrdinalIgnoreCase)) throw new BusinessRuleViolationException("Citizen access is required."); }
    private void EnsureSupervisor() { if (!new[] { "Supervisor", "Admin", "SuperAdmin" }.Contains(_current.Role, StringComparer.OrdinalIgnoreCase)) throw new BusinessRuleViolationException("Supervisor access is required."); }
    private void EnsureAdmin() { if (!new[] { "Admin", "SuperAdmin" }.Contains(_current.Role, StringComparer.OrdinalIgnoreCase)) throw new BusinessRuleViolationException("Admin access is required."); }
    private void EnsureReviewRole() { if (!new[] { "Supervisor", "Admin", "SuperAdmin" }.Contains(_current.Role, StringComparer.OrdinalIgnoreCase)) throw new BusinessRuleViolationException("Review access is required."); }
}
