using System.Text;
using System.Text.Json;
using CivicHero.Backend.Core.DTOs.Disputes;
using CivicHero.Backend.Core.DTOs.Notifications;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using CivicHero.Backend.Infrastructure.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Core.Services;

public sealed class DisputeService : IDisputeService
{
    private const string EvidenceAction = "DISPUTE_EVIDENCE_UPLOADED";
    private const string EvidenceRequestAction = "DISPUTE_EVIDENCE_REQUESTED";
    private const string EvidenceRequestFulfilledAction = "DISPUTE_EVIDENCE_REQUEST_FULFILLED";
    private const string SuperAdminMarker = "[SUPERADMIN_REVIEW]";

    private readonly CivicDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IStorageService _storage;
    private readonly INotificationService _notifications;

    public DisputeService(
        CivicDbContext db,
        ICurrentUserService current,
        IStorageService storage,
        INotificationService notifications)
    {
        _db = db;
        _current = current;
        _storage = storage;
        _notifications = notifications;
    }

    public async Task<IReadOnlyList<DisputeResponse>> MineAsync(CancellationToken ct = default)
    {
        EnsureCitizen();
        var items = await Query().Where(x => x.RaisedByUserId == RequireUser())
            .OrderByDescending(x => x.RaisedAt).ToListAsync(ct);
        return items.Select(MapSummary).ToArray();
    }

    public async Task<IReadOnlyList<DisputeResponse>> OfficerMineAsync(CancellationToken ct = default)
    {
        EnsureOfficer();
        var userId = RequireUser();
        var items = await Query().Where(x => x.Complaint.AssignedOfficerId == userId &&
                (x.Status == DisputeStatus.Raised || x.Status == DisputeStatus.UnderSupervisorReview ||
                 x.Status == DisputeStatus.Appealed || x.Status == DisputeStatus.UnderAdminReview))
            .OrderByDescending(x => x.RaisedAt).Take(100).ToListAsync(ct);
        return items.Select(MapSummary).ToArray();
    }

    public async Task<IReadOnlyList<DisputeResponse>> QueueAsync(bool appealsOnly, CancellationToken ct = default)
    {
        EnsureReviewRole();
        var query = ApplyScope(Query());
        if (appealsOnly)
        {
            query = query.Where(x => x.Status == DisputeStatus.Appealed || x.Status == DisputeStatus.UnderAdminReview);
            if (RoleIs("Admin"))
                query = query.Where(x => x.AdminRemarks == null || !x.AdminRemarks.StartsWith(SuperAdminMarker));
            else if (RoleIs("SuperAdmin"))
                query = query.Where(x => (x.AdminRemarks != null && x.AdminRemarks.StartsWith(SuperAdminMarker)) ||
                                         x.AdminDecision == "EscalatedToSuperAdmin");
        }
        else
        {
            query = query.Where(x => x.Status == DisputeStatus.Raised || x.Status == DisputeStatus.UnderSupervisorReview);
        }
        var items = await query.OrderBy(x => x.RaisedAt).Take(200).ToListAsync(ct);
        return items.Select(MapSummary).ToArray();
    }

    public async Task<DisputeResponse> GetAsync(long id, CancellationToken ct = default)
    {
        var item = await Query().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Dispute was not found.");
        EnsureCanView(item);
        return await MapDetailAsync(item, ct);
    }

    public async Task<IReadOnlyList<DisputeHistoryResponse>> HistoryAsync(long id, CancellationToken ct = default)
    {
        var current = await Query().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Dispute was not found.");
        EnsureCanView(current);
        var query = Query().Where(x => x.ComplaintId == current.ComplaintId);
        if (RoleIs("Supervisor")) query = ApplyScope(query);
        return await query.OrderByDescending(x => x.CycleNumber)
            .Select(x => new DisputeHistoryResponse(x.Id, x.ComplaintId, x.CycleNumber, x.Status.ToString(), x.CitizenRemarks,
                x.SupervisorDecision, x.SupervisorRemarks, x.AdminDecision, x.AdminRemarks, x.RaisedAt, x.ReviewedAt,
                x.ResolvedAt, x.AppealDeadline))
            .ToListAsync(ct);
    }

    public async Task<DisputeResponse> RaiseAsync(long complaintId, RaiseDisputeRequest request, CancellationToken ct = default)
    {
        EnsureCitizen();
        var complaint = await _db.Complaints.Include(x => x.Timeline)
            .SingleOrDefaultAsync(x => x.Id == complaintId && x.CitizenId == RequireUser(), ct)
            ?? throw new NotFoundException("Complaint was not found.");
        if (complaint.Status != ComplaintStatus.VerificationPending && complaint.Status != ComplaintStatus.Resolved)
            throw new BusinessRuleViolationException("Only a resolved complaint awaiting verification can be disputed.");

        var activeExists = await _db.DisputeAuditLogs.AnyAsync(x => x.ComplaintId == complaintId &&
            (x.Status == DisputeStatus.Raised || x.Status == DisputeStatus.UnderSupervisorReview ||
             x.Status == DisputeStatus.Appealed || x.Status == DisputeStatus.UnderAdminReview), ct);
        if (activeExists) throw new BusinessRuleViolationException("An active dispute already exists for this complaint.");

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
        await NotifyReviewersAsync(complaint, "New complaint dispute", request.Reason.Trim(), ct);
        return await GetAsync(item.Id, ct);
    }

    public async Task<DisputeResponse> UploadEvidenceAsync(
        long id,
        IReadOnlyList<IFormFile> evidence,
        long? requestId,
        CancellationToken ct = default)
    {
        if (!RoleIs("Citizen") && !RoleIs("Officer"))
            throw new BusinessRuleViolationException("Only the Citizen or assigned Officer can upload dispute evidence.");
        if (evidence.Count is < 1 or > 5)
            throw new ValidationException(["Upload between one and five dispute evidence files."]);
        if (evidence.Sum(x => x.Length) > 50L * 1024 * 1024)
            throw new ValidationException(["Dispute evidence cannot exceed 50 MB in total."]);

        var item = await Query(true).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Dispute was not found.");
        EnsureCanView(item);
        EnsureEvidenceUploadAllowed(item);

        EvidenceRequestMetadata? linkedRequest = null;
        if (requestId.HasValue)
        {
            var requestAudit = await _db.AuditLogs.AsNoTracking().SingleOrDefaultAsync(x => x.Id == requestId.Value &&
                x.Action == EvidenceRequestAction && x.EntityName == "Dispute" && x.EntityId == id.ToString(), ct)
                ?? throw new NotFoundException("The evidence request was not found.");
            linkedRequest = Parse<EvidenceRequestMetadata>(requestAudit.NewValuesJson)
                ?? throw new BusinessRuleViolationException("The evidence request metadata is invalid.");
            if (!string.Equals(linkedRequest.TargetRole, _current.Role, StringComparison.OrdinalIgnoreCase))
                throw new BusinessRuleViolationException("This evidence request is addressed to another role.");
        }

        var uploaded = new List<string>();
        var persisted = false;
        try
        {
            foreach (var file in evidence)
            {
                var extension = await ValidateEvidenceAsync(file, ct);
                var role = RoleIs("Citizen") ? "Citizen" : "Officer";
                var objectKey = $"disputes/{id}/{role.ToLowerInvariant()}/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{extension}";
                await using var stream = file.OpenReadStream();
                await _storage.UploadAsync(stream, objectKey, file.ContentType,
                    new Dictionary<string, string>
                    {
                        ["dispute-id"] = id.ToString(),
                        ["complaint-id"] = item.ComplaintId.ToString(),
                        ["uploaded-by"] = RequireUser().ToString(),
                        ["source-role"] = role
                    }, ct);
                uploaded.Add(objectKey);

                var metadata = new EvidenceMetadata(
                    objectKey,
                    Path.GetFileName(file.FileName),
                    file.Length,
                    file.ContentType,
                    role,
                    RequireUser(),
                    DateTimeOffset.UtcNow,
                    requestId);
                await _db.AuditLogs.AddAsync(Audit(EvidenceAction, id, metadata), ct);
            }

            item.Complaint.Timeline.Add(T(item.ComplaintId, "DISPUTE_EVIDENCE_ADDED",
                $"{_current.Role} added {evidence.Count} private dispute evidence file(s)."));

            if (requestId.HasValue)
            {
                await _db.AuditLogs.AddAsync(Audit(EvidenceRequestFulfilledAction, id,
                    new EvidenceFulfilmentMetadata(requestId.Value, RequireUser(), _current.Role ?? string.Empty, DateTimeOffset.UtcNow)), ct);
            }
            await _db.SaveChangesAsync(ct);
            persisted = true;

            if (RoleIs("Citizen") && item.Complaint.AssignedOfficerId.HasValue)
                await SendAsync(item.Complaint.AssignedOfficerId.Value, "Citizen added dispute evidence",
                    "The Citizen uploaded additional evidence for a disputed complaint.", id, "/officer/disputes", ct);
            else if (RoleIs("Officer"))
                await SendAsync(item.RaisedByUserId, "Officer added dispute evidence",
                    "The assigned Officer uploaded evidence for your dispute.", id, "/citizen/disputes", ct);

            return await GetAsync(id, ct);
        }
        catch
        {
            if (!persisted)
            {
                foreach (var key in uploaded)
                {
                    try { await _storage.DeleteAsync(key, ct); } catch { }
                }
            }
            throw;
        }
    }

    public async Task<StorageDownload> DownloadEvidenceAsync(long id, long evidenceId, CancellationToken ct = default)
    {
        var item = await Query().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Dispute was not found.");
        EnsureCanView(item);
        var audit = await _db.AuditLogs.AsNoTracking().SingleOrDefaultAsync(x => x.Id == evidenceId &&
            x.Action == EvidenceAction && x.EntityName == "Dispute" && x.EntityId == id.ToString(), ct)
            ?? throw new NotFoundException("Dispute evidence was not found.");
        var metadata = Parse<EvidenceMetadata>(audit.NewValuesJson)
            ?? throw new NotFoundException("Dispute evidence metadata is unavailable.");
        return await _storage.DownloadAsync(metadata.ObjectKey, metadata.FileName, ct)
            ?? throw new NotFoundException("The dispute evidence file is no longer available in storage.");
    }

    public async Task<DisputeEvidenceRequestResponse> RequestEvidenceAsync(
        long id,
        DisputeEvidenceRequest request,
        CancellationToken ct = default)
    {
        EnsureReviewRole();
        var item = await Query().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Dispute was not found.");
        EnsureCanView(item);
        if (!IsActive(item.Status))
            throw new BusinessRuleViolationException("Additional evidence can be requested only while the dispute is under review.");

        var targetRole = NormalizeTargetRole(request.TargetRole);
        var targetUserId = targetRole == "Citizen"
            ? item.RaisedByUserId
            : item.Complaint.AssignedOfficerId ?? throw new BusinessRuleViolationException("No Officer is assigned to this complaint.");
        var metadata = new EvidenceRequestMetadata(
            targetRole,
            request.Message.Trim(),
            DateTimeOffset.UtcNow,
            request.DueAt?.ToUniversalTime(),
            RequireUser());
        var audit = Audit(EvidenceRequestAction, id, metadata);
        await _db.AuditLogs.AddAsync(audit, ct);
        await _db.SaveChangesAsync(ct);

        await SendAsync(targetUserId, "Additional dispute evidence requested", request.Message.Trim(), id,
            targetRole == "Citizen" ? "/citizen/disputes" : "/officer/disputes", ct);
        return new DisputeEvidenceRequestResponse(audit.Id, id, targetRole, metadata.Message, metadata.RequestedAt,
            metadata.DueAt, metadata.RequestedByUserId, false, null);
    }

    public async Task<DisputeResponse> SupervisorDecisionAsync(long id, DisputeDecisionRequest request, CancellationToken ct = default)
    {
        EnsureSupervisor();
        var item = await Query(true).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Dispute was not found.");
        EnsureCanView(item);
        if (item.Status != DisputeStatus.UnderSupervisorReview && item.Status != DisputeStatus.Raised)
            throw new BusinessRuleViolationException("This dispute is not awaiting supervisor review.");

        var decision = NormalizeDecision(request.Decision);
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
                ReopenComplaint(item.Complaint);
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
                item.AdminRemarks = $"{SuperAdminMarker} Escalated by Supervisor: {request.Remarks.Trim()}";
                item.Complaint.Status = ComplaintStatus.Appealed;
                item.Complaint.Timeline.Add(T(item.ComplaintId, "DISPUTE_ESCALATED_TO_SUPERADMIN", request.Remarks.Trim()));
                await NotifySuperAdminsAsync(item, request.Remarks.Trim(), ct);
                break;
            default:
                item.Status = DisputeStatus.Closed;
                item.ResolvedAt = now;
                item.AppealDeadline = now.AddDays(7);
                CloseComplaint(item.Complaint, fraud: false);
                item.Complaint.Timeline.Add(T(item.ComplaintId, "DISPUTE_DECIDED", $"Supervisor {decision}: {request.Remarks.Trim()}"));
                break;
        }

        await _db.SaveChangesAsync(ct);
        await SendAsync(item.RaisedByUserId, "Supervisor dispute decision recorded", request.Remarks.Trim(), id,
            "/citizen/disputes", ct);
        return await GetAsync(id, ct);
    }

    public async Task<DisputeResponse> ReopenRequestAsync(long id, ReopenDisputeRequest request, CancellationToken ct = default)
    {
        EnsureCitizen();
        var item = await Query(true).SingleOrDefaultAsync(x => x.Id == id && x.RaisedByUserId == RequireUser(), ct)
            ?? throw new NotFoundException("Dispute was not found.");
        var now = DateTimeOffset.UtcNow;
        if (item.Status != DisputeStatus.Closed || !item.AppealDeadline.HasValue || item.AppealDeadline < now)
            throw new BusinessRuleViolationException("The reopen grace period has ended.");
        var activeNewerCycle = await _db.DisputeAuditLogs.AnyAsync(x => x.ComplaintId == item.ComplaintId && x.Id != item.Id &&
            x.RaisedAt > item.RaisedAt && (x.Status == DisputeStatus.Raised || x.Status == DisputeStatus.UnderSupervisorReview ||
                                         x.Status == DisputeStatus.Appealed || x.Status == DisputeStatus.UnderAdminReview), ct);
        if (activeNewerCycle) throw new BusinessRuleViolationException("A later dispute review is already active.");

        var next = new DisputeAuditLog
        {
            ComplaintId = item.ComplaintId,
            RaisedByUserId = RequireUser(),
            Status = DisputeStatus.UnderSupervisorReview,
            CycleNumber = await _db.DisputeAuditLogs.CountAsync(x => x.ComplaintId == item.ComplaintId, ct) + 1,
            CitizenRemarks = "Reopen request: " + request.Reason.Trim(),
            RaisedAt = now
        };
        _db.DisputeAuditLogs.Add(next);
        item.Complaint.Status = ComplaintStatus.Disputed;
        item.Complaint.ClosedAt = null;
        item.Complaint.Timeline.Add(T(item.ComplaintId, "DISPUTE_REOPEN_REQUESTED", request.Reason.Trim()));
        await _db.SaveChangesAsync(ct);
        await NotifyReviewersAsync(item.Complaint, "Dispute reopen requested", request.Reason.Trim(), ct);
        return await GetAsync(next.Id, ct);
    }

    public async Task<DisputeResponse> AppealAsync(long id, AppealDisputeRequest request, CancellationToken ct = default)
    {
        EnsureCitizen();
        var item = await Query(true).SingleOrDefaultAsync(x => x.Id == id && x.RaisedByUserId == RequireUser(), ct)
            ?? throw new NotFoundException("Dispute was not found.");
        if (item.Status != DisputeStatus.Closed || !item.AppealDeadline.HasValue || item.AppealDeadline < DateTimeOffset.UtcNow)
            throw new BusinessRuleViolationException("The appeal window is closed.");
        var laterReviewExists = await _db.DisputeAuditLogs.AnyAsync(x => x.ComplaintId == item.ComplaintId && x.Id != item.Id &&
            x.RaisedAt > item.RaisedAt && (x.Status == DisputeStatus.Raised || x.Status == DisputeStatus.UnderSupervisorReview ||
                                         x.Status == DisputeStatus.Appealed || x.Status == DisputeStatus.UnderAdminReview), ct);
        if (laterReviewExists) throw new BusinessRuleViolationException("A later dispute review is already active.");
        item.Status = DisputeStatus.UnderAdminReview;
        item.AdminRemarks = "Citizen appeal: " + request.Remarks.Trim();
        item.Complaint.Status = ComplaintStatus.Appealed;
        item.Complaint.Timeline.Add(T(item.ComplaintId, "DISPUTE_APPEALED", request.Remarks.Trim()));
        await _db.SaveChangesAsync(ct);
        await NotifyAdminsAsync(item, "Citizen appeal submitted", request.Remarks.Trim(), superAdminOnly: false, ct);
        return await GetAsync(id, ct);
    }

    public async Task<DisputeResponse> AdminDecisionAsync(long id, DisputeDecisionRequest request, CancellationToken ct = default)
    {
        EnsureAdminOnly();
        var item = await Query(true).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Dispute was not found.");
        if (item.Status != DisputeStatus.UnderAdminReview && item.Status != DisputeStatus.Appealed)
            throw new BusinessRuleViolationException("This dispute is not awaiting Admin review.");
        if (RequiresSuperAdmin(item))
            throw new BusinessRuleViolationException("This appeal requires a SuperAdmin final decision.");

        var decision = NormalizeDecision(request.Decision);
        if (decision == "EscalateToSuperAdmin")
        {
            item.AdminDecision = "EscalatedToSuperAdmin";
            item.AdminRemarks = $"{SuperAdminMarker} Escalated by Admin: {request.Remarks.Trim()}";
            item.ReviewedByUserId = RequireUser();
            item.ReviewedAt = DateTimeOffset.UtcNow;
            item.ResolvedAt = null;
            item.Status = DisputeStatus.UnderAdminReview;
            item.Complaint.Status = ComplaintStatus.Appealed;
            item.Complaint.Timeline.Add(T(item.ComplaintId, "APPEAL_ESCALATED_TO_SUPERADMIN", request.Remarks.Trim()));
            await _db.SaveChangesAsync(ct);
            await NotifySuperAdminsAsync(item, request.Remarks.Trim(), ct);
            return await GetAsync(id, ct);
        }

        ApplyFinalDecision(item, decision, request.Remarks.Trim(), "ADMIN_DISPUTE_DECISION", "Admin");
        await _db.SaveChangesAsync(ct);
        await SendAsync(item.RaisedByUserId, "Administrative appeal decision recorded", request.Remarks.Trim(), id,
            "/citizen/disputes", ct);
        return await GetAsync(id, ct);
    }

    public async Task<DisputeResponse> SuperAdminDecisionAsync(long id, DisputeDecisionRequest request, CancellationToken ct = default)
    {
        EnsureSuperAdmin();
        var item = await Query(true).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Dispute was not found.");
        if ((item.Status != DisputeStatus.UnderAdminReview && item.Status != DisputeStatus.Appealed) || !RequiresSuperAdmin(item))
            throw new BusinessRuleViolationException("This dispute is not awaiting a SuperAdmin final appeal decision.");

        var decision = NormalizeDecision(request.Decision);
        if (decision == "AdditionalInvestigation")
        {
            item.AdminDecision = "SuperAdmin:AdditionalInvestigation";
            item.AdminRemarks = $"{SuperAdminMarker} {request.Remarks.Trim()}";
            item.ReviewedByUserId = RequireUser();
            item.ReviewedAt = DateTimeOffset.UtcNow;
            item.ResolvedAt = null;
            item.Status = DisputeStatus.UnderAdminReview;
            item.Complaint.Status = ComplaintStatus.Disputed;
            item.Complaint.Timeline.Add(T(item.ComplaintId, "SUPERADMIN_ADDITIONAL_INVESTIGATION", request.Remarks.Trim()));
            await _db.SaveChangesAsync(ct);
            return await GetAsync(id, ct);
        }

        ApplyFinalDecision(item, decision, request.Remarks.Trim(), "SUPERADMIN_FINAL_APPEAL_DECISION", "SuperAdmin");
        item.AdminDecision = "SuperAdmin:" + decision;
        await _db.SaveChangesAsync(ct);
        await SendAsync(item.RaisedByUserId, "Final appeal decision recorded", request.Remarks.Trim(), id,
            "/citizen/disputes", ct);
        if (item.Complaint.AssignedOfficerId.HasValue)
            await SendAsync(item.Complaint.AssignedOfficerId.Value, "Final dispute decision recorded", request.Remarks.Trim(), id,
                "/officer/disputes", ct);
        return await GetAsync(id, ct);
    }

    private async Task<DisputeResponse> MapDetailAsync(DisputeAuditLog item, CancellationToken ct)
    {
        var evidence = await LoadEvidenceAsync(item.Id, ct);
        var requests = await LoadRequestsAsync(item.Id, ct);
        var laterReviewExists = await _db.DisputeAuditLogs.AsNoTracking().AnyAsync(x => x.ComplaintId == item.ComplaintId &&
            x.Id != item.Id && x.RaisedAt > item.RaisedAt &&
            (x.Status == DisputeStatus.Raised || x.Status == DisputeStatus.UnderSupervisorReview ||
             x.Status == DisputeStatus.Appealed || x.Status == DisputeStatus.UnderAdminReview), ct);
        var canReopen = RoleIs("Citizen") && item.RaisedByUserId == _current.UserId && !laterReviewExists &&
                        item.Status == DisputeStatus.Closed && item.AppealDeadline >= DateTimeOffset.UtcNow;
        var active = IsActive(item.Status);
        var citizenUpload = RoleIs("Citizen") && item.RaisedByUserId == _current.UserId &&
                            (active || (item.Status == DisputeStatus.Closed && item.AppealDeadline >= DateTimeOffset.UtcNow));
        var officerUpload = RoleIs("Officer") && item.Complaint.AssignedOfficerId == _current.UserId && active;
        return new DisputeResponse(item.Id, item.ComplaintId, Reference(item.ComplaintId), item.Complaint.Title,
            item.Status.ToString(), item.CycleNumber, item.CitizenRemarks, item.SupervisorDecision, item.SupervisorRemarks,
            item.AdminDecision, item.AdminRemarks, item.RaisedAt, item.AppealDeadline,
            RoleIs("Citizen") && !laterReviewExists && item.Status == DisputeStatus.Closed && item.AppealDeadline >= DateTimeOffset.UtcNow,
            RequiresSuperAdmin(item), item.AppealDeadline, canReopen, citizenUpload, officerUpload, evidence, requests);
    }

    private DisputeResponse MapSummary(DisputeAuditLog item) => new(item.Id, item.ComplaintId,
        Reference(item.ComplaintId), item.Complaint.Title, item.Status.ToString(), item.CycleNumber,
        item.CitizenRemarks, item.SupervisorDecision, item.SupervisorRemarks, item.AdminDecision, item.AdminRemarks,
        item.RaisedAt, item.AppealDeadline,
        RoleIs("Citizen") && item.Status == DisputeStatus.Closed && item.AppealDeadline >= DateTimeOffset.UtcNow,
        RequiresSuperAdmin(item), item.AppealDeadline,
        RoleIs("Citizen") && item.RaisedByUserId == _current.UserId && item.Status == DisputeStatus.Closed &&
        item.AppealDeadline >= DateTimeOffset.UtcNow);

    private async Task<IReadOnlyList<DisputeEvidenceResponse>> LoadEvidenceAsync(long disputeId, CancellationToken ct)
    {
        var audits = await _db.AuditLogs.AsNoTracking().Where(x => x.Action == EvidenceAction &&
                x.EntityName == "Dispute" && x.EntityId == disputeId.ToString())
            .OrderBy(x => x.CreatedAt).ToListAsync(ct);
        return audits.Select(a => (Audit: a, Meta: Parse<EvidenceMetadata>(a.NewValuesJson)))
            .Where(x => x.Meta is not null)
            .Select(x => new DisputeEvidenceResponse(x.Audit.Id, disputeId, x.Meta!.FileName, x.Meta.FileSize,
                x.Meta.MimeType, x.Meta.SourceRole, x.Meta.UploadedByUserId, x.Meta.UploadedAt, x.Meta.RequestId,
                $"/api/v1/disputes/{disputeId}/evidence/{x.Audit.Id}"))
            .ToArray();
    }

    private async Task<IReadOnlyList<DisputeEvidenceRequestResponse>> LoadRequestsAsync(long disputeId, CancellationToken ct)
    {
        var requests = await _db.AuditLogs.AsNoTracking().Where(x => x.Action == EvidenceRequestAction &&
                x.EntityName == "Dispute" && x.EntityId == disputeId.ToString())
            .OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        var fulfilled = await _db.AuditLogs.AsNoTracking().Where(x => x.Action == EvidenceRequestFulfilledAction &&
                x.EntityName == "Dispute" && x.EntityId == disputeId.ToString())
            .ToListAsync(ct);
        var fulfilments = fulfilled.Select(x => Parse<EvidenceFulfilmentMetadata>(x.NewValuesJson))
            .Where(x => x is not null).Cast<EvidenceFulfilmentMetadata>().ToArray();
        return requests.Select(a => (Audit: a, Meta: Parse<EvidenceRequestMetadata>(a.NewValuesJson)))
            .Where(x => x.Meta is not null)
            .Select(x =>
            {
                var completion = fulfilments.OrderByDescending(f => f.FulfilledAt)
                    .FirstOrDefault(f => f.RequestId == x.Audit.Id);
                return new DisputeEvidenceRequestResponse(x.Audit.Id, disputeId, x.Meta!.TargetRole, x.Meta.Message,
                    x.Meta.RequestedAt, x.Meta.DueAt, x.Meta.RequestedByUserId, completion is not null,
                    completion?.FulfilledAt);
            }).ToArray();
    }

    private IQueryable<DisputeAuditLog> Query(bool track = false)
    {
        var query = _db.DisputeAuditLogs.Include(x => x.Complaint).ThenInclude(x => x.Department)
            .Include(x => x.Complaint).ThenInclude(x => x.Ward)
            .Include(x => x.Complaint).ThenInclude(x => x.Timeline);
        return track ? query.AsTracking() : query.AsNoTracking();
    }

    private IQueryable<DisputeAuditLog> ApplyScope(IQueryable<DisputeAuditLog> query)
    {
        if (RoleIs("Supervisor"))
        {
            query = query.Where(x => x.Complaint.DepartmentId == _current.DepartmentId);
            if (_current.WardId.HasValue) query = query.Where(x => x.Complaint.WardId == _current.WardId);
        }
        return query;
    }

    private void EnsureCanView(DisputeAuditLog item)
    {
        if (RoleIs("Citizen") && item.RaisedByUserId != RequireUser())
            throw new NotFoundException("Dispute was not found.");
        if (RoleIs("Officer") && item.Complaint.AssignedOfficerId != RequireUser())
            throw new NotFoundException("Dispute was not found.");
        if (RoleIs("Supervisor") &&
            (item.Complaint.DepartmentId != _current.DepartmentId ||
             (_current.WardId.HasValue && item.Complaint.WardId != _current.WardId)))
            throw new NotFoundException("Dispute was not found.");
        if (!new[] { "Citizen", "Officer", "Supervisor", "Admin", "SuperAdmin" }
            .Contains(_current.Role, StringComparer.OrdinalIgnoreCase))
            throw new NotFoundException("Dispute was not found.");
    }

    private void EnsureEvidenceUploadAllowed(DisputeAuditLog item)
    {
        if (RoleIs("Citizen"))
        {
            if (item.RaisedByUserId != RequireUser()) throw new NotFoundException("Dispute was not found.");
            if (IsActive(item.Status) || (item.Status == DisputeStatus.Closed && item.AppealDeadline >= DateTimeOffset.UtcNow)) return;
        }
        else if (RoleIs("Officer") && item.Complaint.AssignedOfficerId == RequireUser() && IsActive(item.Status)) return;
        throw new BusinessRuleViolationException("Evidence cannot be added at the current dispute stage.");
    }

    private void ApplyFinalDecision(DisputeAuditLog item, string decision, string remarks, string eventType, string actor)
    {
        var now = DateTimeOffset.UtcNow;
        item.ReviewedByUserId = RequireUser();
        item.AdminDecision = decision;
        item.AdminRemarks = remarks;
        item.ReviewedAt = now;
        item.ResolvedAt = now;
        item.Status = DisputeStatus.Resolved;
        if (decision is "Rework" or "CitizenCorrect" or "Reopen") ReopenComplaint(item.Complaint);
        else if (decision == "Fraud") CloseComplaint(item.Complaint, fraud: true);
        else CloseComplaint(item.Complaint, fraud: false);
        item.Complaint.Timeline.Add(T(item.ComplaintId, eventType, $"{actor} {decision}: {remarks}"));
    }

    private static void ReopenComplaint(Complaint complaint)
    {
        complaint.Status = ComplaintStatus.InProgress;
        complaint.ClosedAt = null;
        complaint.ResolvedAt = null;
    }

    private static void CloseComplaint(Complaint complaint, bool fraud)
    {
        complaint.Status = fraud ? ComplaintStatus.ClosedFraud : ComplaintStatus.Closed;
        complaint.ClosedAt = DateTimeOffset.UtcNow;
    }

    private async Task NotifyReviewersAsync(Complaint complaint, string title, string message, CancellationToken ct)
    {
        var reviewerIds = await _db.Users.AsNoTracking().Where(x => x.IsActive && !x.IsDeleted &&
                (x.Role == UserRole.Supervisor || x.Role == UserRole.Admin || x.Role == UserRole.SuperAdmin) &&
                (x.Role != UserRole.Supervisor || (x.DepartmentId == complaint.DepartmentId &&
                    (!x.WardId.HasValue || x.WardId == complaint.WardId))))
            .Select(x => x.Id).ToListAsync(ct);
        foreach (var id in reviewerIds)
            await SendAsync(id, title, message, complaint.Id, "/supervisor/disputes", ct);
    }

    private Task NotifySuperAdminsAsync(DisputeAuditLog item, string message, CancellationToken ct) =>
        NotifyAdminsAsync(item, "Dispute escalated for final appeal", message, true, ct);

    private async Task NotifyAdminsAsync(DisputeAuditLog item, string title, string message, bool superAdminOnly, CancellationToken ct)
    {
        var roles = superAdminOnly ? new[] { UserRole.SuperAdmin } : new[] { UserRole.Admin };
        var ids = await _db.Users.AsNoTracking().Where(x => x.IsActive && !x.IsDeleted && roles.Contains(x.Role))
            .Select(x => x.Id).ToListAsync(ct);
        foreach (var id in ids) await SendAsync(id, title, message, item.Id, "/admin/appeals", ct);
    }

    private Task SendAsync(long userId, string title, string message, long disputeId, string actionUrl, CancellationToken ct) =>
        _notifications.SendAsync(new NotificationDispatchRequest(userId, title, message, nameof(NotificationType.DisputeUpdate),
            "Dispute", disputeId, actionUrl), ct);

    private AuditLog Audit<T>(string action, long disputeId, T values) => new()
    {
        UserId = RequireUser(),
        UserEmail = _current.Email,
        UserRole = _current.Role,
        Action = action,
        EntityName = "Dispute",
        EntityId = disputeId.ToString(),
        NewValuesJson = JsonSerializer.Serialize(values),
        Severity = "Information",
        Success = true,
        HttpStatusCode = 200,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private ComplaintTimeline T(long complaintId, string eventType, string description) => new()
    {
        ComplaintId = complaintId,
        UserId = RequireUser(),
        EventType = eventType,
        Description = description,
        Timestamp = DateTimeOffset.UtcNow
    };

    private static bool IsActive(DisputeStatus status) => status is DisputeStatus.Raised or
        DisputeStatus.UnderSupervisorReview or DisputeStatus.Appealed or DisputeStatus.UnderAdminReview;

    private static bool RequiresSuperAdmin(DisputeAuditLog item) =>
        item.AdminDecision == "EscalatedToSuperAdmin" ||
        (!string.IsNullOrWhiteSpace(item.AdminRemarks) && item.AdminRemarks.StartsWith(SuperAdminMarker, StringComparison.Ordinal));

    private static string NormalizeDecision(string value)
    {
        var decision = value.Trim();
        if (decision.Equals("OrderRework", StringComparison.OrdinalIgnoreCase)) return "Rework";
        if (decision.Equals("CloseDispute", StringComparison.OrdinalIgnoreCase)) return "Close";
        if (decision.Equals("UpholdResolution", StringComparison.OrdinalIgnoreCase) || decision.Equals("Uphold", StringComparison.OrdinalIgnoreCase))
            return "OfficerEvidenceSufficient";
        var allowed = new[] { "Rework", "Close", "Fraud", "CitizenCorrect", "OfficerEvidenceSufficient", "Reopen",
            "AdditionalInvestigation", "EscalateToSuperAdmin" };
        return allowed.FirstOrDefault(x => x.Equals(decision, StringComparison.OrdinalIgnoreCase))
            ?? throw new ValidationException(["Select a supported dispute decision."]);
    }

    private static string NormalizeTargetRole(string value) => value.Trim().ToLowerInvariant() switch
    {
        "citizen" => "Citizen",
        "officer" => "Officer",
        _ => throw new ValidationException(["Evidence can be requested from the Citizen or assigned Officer."])
    };

    private static string Reference(long complaintId) => "CH-" + complaintId.ToString("D6");
    private static T? Parse<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;
        try { return JsonSerializer.Deserialize<T>(json); } catch { return default; }
    }

    private static async Task<string> ValidateEvidenceAsync(IFormFile file, CancellationToken ct)
    {
        if (file.Length <= 0 || file.Length > 15L * 1024 * 1024)
            throw new ValidationException(["Each dispute evidence file must be between 1 byte and 15 MB."]);
        var mime = (file.ContentType ?? string.Empty).Trim().ToLowerInvariant();
        var allowed = mime switch
        {
            "image/jpeg" => new[] { ".jpg", ".jpeg" },
            "image/png" => new[] { ".png" },
            "image/webp" => new[] { ".webp" },
            "video/mp4" => new[] { ".mp4" },
            "video/webm" => new[] { ".webm" },
            "video/quicktime" => new[] { ".mov" },
            "application/pdf" => new[] { ".pdf" },
            "application/msword" => new[] { ".doc" },
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => new[] { ".docx" },
            _ => Array.Empty<string>()
        };
        if (allowed.Length == 0)
            throw new ValidationException(["Dispute evidence must be JPEG, PNG, WebP, MP4, WebM, MOV, PDF, DOC, or DOCX."]);
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(extension, StringComparer.OrdinalIgnoreCase))
            throw new ValidationException(["The file extension does not match its declared content type."]);

        await using var stream = file.OpenReadStream();
        var header = new byte[16];
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length), ct);
        var valid = mime switch
        {
            "image/jpeg" => read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            "image/png" => read >= 8 && header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            "image/webp" => read >= 12 && Encoding.ASCII.GetString(header, 0, 4) == "RIFF" && Encoding.ASCII.GetString(header, 8, 4) == "WEBP",
            "video/mp4" or "video/quicktime" => read >= 8 && Encoding.ASCII.GetString(header, 4, 4) == "ftyp",
            "video/webm" => read >= 4 && header[0] == 0x1A && header[1] == 0x45 && header[2] == 0xDF && header[3] == 0xA3,
            "application/pdf" => read >= 4 && Encoding.ASCII.GetString(header, 0, 4) == "%PDF",
            "application/msword" => read >= 8 && header[..8].SequenceEqual(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }),
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => read >= 4 && header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04,
            _ => false
        };
        if (!valid) throw new ValidationException(["The dispute evidence file signature is invalid."]);
        return extension;
    }

    private long RequireUser() => _current.UserId ?? throw new BusinessRuleViolationException("Authenticated user is required.");
    private bool RoleIs(string role) => string.Equals(_current.Role, role, StringComparison.OrdinalIgnoreCase);
    private void EnsureCitizen() { if (!RoleIs("Citizen")) throw new BusinessRuleViolationException("Citizen access is required."); }
    private void EnsureOfficer() { if (!RoleIs("Officer")) throw new BusinessRuleViolationException("Officer access is required."); }
    private void EnsureSupervisor() { if (!new[] { "Supervisor", "Admin", "SuperAdmin" }.Contains(_current.Role, StringComparer.OrdinalIgnoreCase)) throw new BusinessRuleViolationException("Supervisor access is required."); }
    private void EnsureAdminOnly() { if (!RoleIs("Admin")) throw new BusinessRuleViolationException("Admin access is required. SuperAdmin final decisions use the dedicated endpoint."); }
    private void EnsureSuperAdmin() { if (!RoleIs("SuperAdmin")) throw new BusinessRuleViolationException("SuperAdmin access is required."); }
    private void EnsureReviewRole() { if (!new[] { "Supervisor", "Admin", "SuperAdmin" }.Contains(_current.Role, StringComparer.OrdinalIgnoreCase)) throw new BusinessRuleViolationException("Review access is required."); }

    private sealed record EvidenceMetadata(string ObjectKey, string FileName, long FileSize, string MimeType,
        string SourceRole, long UploadedByUserId, DateTimeOffset UploadedAt, long? RequestId);
    private sealed record EvidenceRequestMetadata(string TargetRole, string Message, DateTimeOffset RequestedAt,
        DateTimeOffset? DueAt, long RequestedByUserId);
    private sealed record EvidenceFulfilmentMetadata(long RequestId, long FulfilledByUserId, string FulfilledByRole,
        DateTimeOffset FulfilledAt);
}
