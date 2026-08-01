using System.Text.Json;
using CivicHero.Backend.Core.DTOs.Notifications;
using CivicHero.Backend.Core.DTOs.Verification;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using CivicHero.Backend.Infrastructure.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Core.Services;

public sealed class VerificationService : IVerificationService
{
    private const double AllowedDistanceMetres = 500;
    private const long MaximumEvidenceBytes = 5 * 1024 * 1024;
    private const int MaximumEvidenceFilesPerUpload = 5;
    private const int MaximumCitizenEvidenceFiles = 10;
    private const string OfficerReminderEvent = "OFFICER_VERIFICATION_REMINDER";
    private const string StaffReminderEvent = "SUPERVISOR_VERIFICATION_REMINDER";
    private const string LegacyReminderEvent = "VERIFICATION_REMINDER";

    private static readonly string[] ReminderEvents =
    [
        OfficerReminderEvent,
        StaffReminderEvent,
        LegacyReminderEvent
    ];

    private readonly CivicDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IStorageService _storage;
    private readonly INotificationService _notifications;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public VerificationService(
        CivicDbContext db,
        ICurrentUserService current,
        IStorageService storage,
        INotificationService notifications,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _current = current;
        _storage = storage;
        _notifications = notifications;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IReadOnlyList<VerificationQueueItem>> GetPendingAsync(CancellationToken ct = default)
    {
        EnsureRole("Citizen");
        var userId = RequireUser();
        await EnsureRowsAsync(ct);

        var rows = await QueryQueue()
            .Where(x => x.Complaint.CitizenId == userId && x.Decision == VerificationDecision.Pending)
            .OrderBy(x => x.DueAt)
            .ToListAsync(ct);

        return rows.Select(x => QueueItem(x, false, null)).ToArray();
    }

    public async Task<IReadOnlyList<VerificationQueueItem>> GetSupervisorQueueAsync(bool overdueOnly, CancellationToken ct = default)
    {
        EnsureSupervisor();
        await EnsureRowsAsync(ct);

        var source = ApplyScope(QueryQueue()
            .Where(x => x.Decision == VerificationDecision.Pending || x.Complaint.Status == ComplaintStatus.Disputed));

        if (overdueOnly)
            source = source.Where(x => x.Decision == VerificationDecision.Pending && x.DueAt < DateTimeOffset.UtcNow);

        var rows = await source.OrderBy(x => x.DueAt).Take(200).ToListAsync(ct);
        var complaintIds = rows.Select(x => x.ComplaintId).ToArray();
        var reviewComplaintIds = complaintIds.Length == 0
            ? new HashSet<long>()
            : (await _db.DisputeAuditLogs.AsNoTracking()
                .Where(x => complaintIds.Contains(x.ComplaintId) &&
                            (x.Status == DisputeStatus.Raised || x.Status == DisputeStatus.UnderSupervisorReview))
                .Select(x => x.ComplaintId)
                .Distinct()
                .ToListAsync(ct))
                .ToHashSet();

        var reminderStates = await GetReminderStatesAsync(rows, ct);

        return rows
            .Where(x => x.Decision == VerificationDecision.Pending || reviewComplaintIds.Contains(x.ComplaintId))
            .Select(x => QueueItem(
                x,
                reviewComplaintIds.Contains(x.ComplaintId),
                reminderStates.GetValueOrDefault(x.ComplaintId)))
            .ToArray();
    }

    public async Task<IReadOnlyList<VerificationHistoryItem>> GetHistoryAsync(CancellationToken ct = default)
    {
        EnsureHistoryRole();
        await EnsureRowsAsync(ct);

        var query = QueryQueue();
        if (string.Equals(_current.Role, "Citizen", StringComparison.OrdinalIgnoreCase))
            query = query.Where(x => x.CitizenId == RequireUser());
        else
            query = ApplyScope(query);

        var rows = await query.OrderByDescending(x => x.CompletedAt ?? x.DueAt).Take(300).ToListAsync(ct);
        var complaintIds = rows.Select(x => x.ComplaintId).ToArray();
        List<DisputeAuditLog> disputes = [];
        if (complaintIds.Length > 0)
        {
            disputes = await _db.DisputeAuditLogs.AsNoTracking()
                .Where(x => complaintIds.Contains(x.ComplaintId))
                .OrderByDescending(x => x.CycleNumber)
                .ThenByDescending(x => x.RaisedAt)
                .ToListAsync(ct);
        }

        var disputesByComplaint = disputes
            .GroupBy(x => x.ComplaintId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(item => item.CycleNumber).ThenByDescending(item => item.RaisedAt).ToArray());

        return rows.Select(x =>
        {
            disputesByComplaint.TryGetValue(x.ComplaintId, out var complaintDisputes);
            var dispute = complaintDisputes?.FirstOrDefault();
            var cycles = complaintDisputes?.Select(item => new VerificationDecisionCycleItem(
                item.CycleNumber,
                CitizenDecisionFromRemarks(item.CitizenRemarks),
                item.CitizenRemarks,
                item.Status.ToString(),
                item.RaisedAt,
                item.SupervisorDecision,
                item.SupervisorRemarks,
                item.ReviewedAt,
                item.ResolvedAt)).ToArray() ?? Array.Empty<VerificationDecisionCycleItem>();

            return new VerificationHistoryItem(
                x.Id,
                x.ComplaintId,
                Reference(x.ComplaintId),
                x.Complaint.Title,
                x.Complaint.Department.Name,
                x.Complaint.Ward.Name,
                x.Complaint.Status.ToString(),
                x.Decision.ToString(),
                x.Rating,
                x.Remarks,
                x.DueAt,
                x.CompletedAt,
                x.Complaint.Images.Count(IsCitizenVerificationEvidence),
                dispute?.SupervisorDecision,
                dispute?.SupervisorRemarks,
                dispute?.ReviewedAt,
                cycles);
        }).ToArray();
    }

    public async Task<VerificationResponse> GetAsync(long id, CancellationToken ct = default)
    {
        await EnsureRowsAsync(ct);
        var row = await QueryQueue().SingleOrDefaultAsync(x => x.ComplaintId == id, ct)
            ?? throw new NotFoundException("Verification was not found.");
        EnsureCanView(row);
        var reminderState = IsReminderRole()
            ? await GetReminderStateAsync(row, ct)
            : null;
        return Map(row, reminderState);
    }

    public async Task<object> CheckGeoAsync(long id, GeoVerifyRequest request, CancellationToken ct = default)
    {
        var row = await LoadCitizenAsync(id, ct);
        var distance = CalculateDistance(
            (double)row.Complaint.Latitude,
            (double)row.Complaint.Longitude,
            (double)request.Latitude,
            (double)request.Longitude);

        return new
        {
            distanceMetres = Math.Round(distance, 1),
            withinAllowedRange = distance <= AllowedDistanceMetres,
            allowedDistanceMetres = AllowedDistanceMetres
        };
    }

    public async Task<VerificationResponse> VerifyAsync(long id, VerifyComplaintRequest request, CancellationToken ct = default)
    {
        var row = await LoadCitizenAsync(id, ct);
        if (row.Decision != VerificationDecision.Pending || row.Complaint.Status != ComplaintStatus.VerificationPending)
            throw new BusinessRuleViolationException("This complaint is no longer awaiting an initial verification decision.");

        await ApplyCitizenDecisionAsync(row, request, ct);
        await _db.SaveChangesAsync(ct);
        return Map(row);
    }

    public async Task<VerificationResponse> AmendAsync(long id, VerifyComplaintRequest request, CancellationToken ct = default)
    {
        var row = await LoadCitizenAsync(id, ct);
        var pendingReview = await RequirePendingCitizenReviewAsync(id, ct);

        ClosePendingCitizenReview(pendingReview, "CitizenAmended", "Citizen replaced the pending verification decision.");
        ResetForCitizenDecision(row);
        AddTimeline(row.Complaint, "VERIFICATION_DECISION_AMENDED", "Citizen amended the pending verification decision before supervisor review.");

        await ApplyCitizenDecisionAsync(row, request, ct);
        await _db.SaveChangesAsync(ct);
        return Map(row);
    }

    public async Task<VerificationResponse> WithdrawAsync(long id, CancellationToken ct = default)
    {
        var row = await LoadCitizenAsync(id, ct);
        var pendingReview = await RequirePendingCitizenReviewAsync(id, ct);

        ClosePendingCitizenReview(pendingReview, "CitizenWithdrawn", "Citizen withdrew the pending verification decision.");
        ResetForCitizenDecision(row);
        AddTimeline(row.Complaint, "VERIFICATION_DECISION_WITHDRAWN", "Citizen withdrew the pending decision. Verification is open again.");

        await _db.SaveChangesAsync(ct);
        return Map(row);
    }

    public async Task<VerificationResponse> UploadEvidenceAsync(long id, VerificationEvidenceUploadRequest request, CancellationToken ct = default)
    {
        var row = await LoadCitizenAsync(id, ct);
        await EnsureCitizenCanStillActAsync(row, ct);

        if (request.Evidence.Count is < 1 or > MaximumEvidenceFilesPerUpload)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException([
                $"Upload between one and {MaximumEvidenceFilesPerUpload} verification images at a time."
            ]);

        var existingCount = row.Complaint.Images.Count(IsCitizenVerificationEvidence);
        if (existingCount + request.Evidence.Count > MaximumCitizenEvidenceFiles)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException([
                $"A maximum of {MaximumCitizenEvidenceFiles} citizen verification images is allowed per complaint."
            ]);

        var uploadedKeys = new List<string>();
        try
        {
            foreach (var file in request.Evidence)
            {
                var extension = await ValidateEvidenceAsync(file, ct);
                var objectKey = $"verification-evidence/{id}/{RequireUser()}/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{extension}";

                await using var stream = file.OpenReadStream();
                await _storage.UploadAsync(
                    stream,
                    objectKey,
                    file.ContentType,
                    new Dictionary<string, string>
                    {
                        ["complaint-id"] = id.ToString(),
                        ["citizen-id"] = RequireUser().ToString(),
                        ["evidence-type"] = "citizen-verification"
                    },
                    ct);

                uploadedKeys.Add(objectKey);
                row.Complaint.Images.Add(new ComplaintImage
                {
                    S3Key = objectKey,
                    FileName = Path.GetFileName(file.FileName),
                    FileSize = file.Length,
                    MimeType = file.ContentType,
                    IsResolutionEvidence = false,
                    UploadedAt = DateTimeOffset.UtcNow
                });
            }

            AddTimeline(row.Complaint, "CITIZEN_VERIFICATION_EVIDENCE_ADDED",
                $"Citizen uploaded {request.Evidence.Count} verification evidence image(s).");
            await _db.SaveChangesAsync(ct);
            return Map(row);
        }
        catch
        {
            foreach (var key in uploadedKeys)
            {
                try { await _storage.DeleteAsync(key, ct); }
                catch { }
            }
            throw;
        }
    }

    public async Task<VerificationResponse> RemindAsync(long id, CancellationToken ct = default)
    {
        EnsureReminderRole();
        await EnsureRowsAsync(ct);
        var row = await QueryQueue(true).SingleOrDefaultAsync(x => x.ComplaintId == id, ct)
            ?? throw new NotFoundException("Verification was not found.");
        EnsureCanView(row);

        if (row.Decision != VerificationDecision.Pending || row.Complaint.Status != ComplaintStatus.VerificationPending)
            throw new BusinessRuleViolationException("A reminder can only be sent while the Citizen verification decision is pending.");

        if (string.Equals(_current.Role, "Officer", StringComparison.OrdinalIgnoreCase) &&
            row.Complaint.AssignedOfficerId != RequireUser())
            throw new NotFoundException("Verification was not found.");

        var before = await GetReminderStateAsync(row, ct);
        if (!before.Allowed)
            throw new BusinessRuleViolationException(before.UnavailableReason ?? "A verification reminder cannot be sent right now.");

        var now = DateTimeOffset.UtcNow;
        var actorRole = _current.Role ?? "Authorized staff";
        var eventType = string.Equals(actorRole, "Officer", StringComparison.OrdinalIgnoreCase)
            ? OfficerReminderEvent
            : StaffReminderEvent;

        row.ReminderSentAt = now;
        AddTimeline(
            row.Complaint,
            eventType,
            $"{actorRole} sent a controlled verification reminder to the Citizen.");
        AddReminderAudit(row, before, now);

        await _notifications.SendAsync(new NotificationDispatchRequest(
            row.CitizenId,
            "Verification reminder",
            $"Please review the resolution for {Reference(row.ComplaintId)}: {row.Complaint.Title}. Your verification window ends {row.DueAt.ToUniversalTime():dd MMM yyyy, HH:mm} UTC.",
            nameof(NotificationType.VerificationRequired),
            "Complaint",
            row.ComplaintId,
            "/citizen/verifications"), ct);

        await _db.SaveChangesAsync(ct);

        var after = VerificationReminderPolicy.Evaluate(
            _current.Role,
            before.SentCount + 1,
            now,
            now);
        return Map(row, after);
    }

    public async Task<object> SupervisorDecisionAsync(long id, SupervisorVerificationDecisionRequest request, CancellationToken ct = default)
    {
        EnsureSupervisor();
        await EnsureRowsAsync(ct);

        var row = await QueryQueue(true).SingleOrDefaultAsync(x => x.ComplaintId == id, ct)
            ?? throw new NotFoundException("Verification was not found.");
        EnsureCanView(row);

        var dispute = await _db.DisputeAuditLogs
            .Where(x => x.ComplaintId == id &&
                        (x.Status == DisputeStatus.Raised || x.Status == DisputeStatus.UnderSupervisorReview))
            .OrderByDescending(x => x.CycleNumber)
            .ThenByDescending(x => x.RaisedAt)
            .FirstOrDefaultAsync(ct)
            ?? throw new BusinessRuleViolationException("This verification does not have a pending supervisor review.");

        var now = DateTimeOffset.UtcNow;
        dispute.ReviewedByUserId = RequireUser();
        dispute.ReviewedAt = now;
        dispute.SupervisorRemarks = request.Remarks.Trim();

        if (request.ApproveCitizen)
        {
            dispute.SupervisorDecision = "CitizenCorrect";
            dispute.Status = DisputeStatus.ReworkOrdered;
            dispute.ResolvedAt = now;
            row.Complaint.Status = ComplaintStatus.InProgress;
            row.Complaint.ClosedAt = null;
            AddTimeline(row.Complaint, "CITIZEN_VERIFICATION_UPHELD",
                "Supervisor agreed with the citizen and returned the complaint for rework: " + request.Remarks.Trim());
        }
        else
        {
            dispute.SupervisorDecision = "OfficerEvidenceSufficient";
            dispute.Status = DisputeStatus.Closed;
            dispute.ResolvedAt = now;
            dispute.AppealDeadline = now.AddDays(7);
            row.Complaint.Status = ComplaintStatus.Closed;
            row.Complaint.ClosedAt = now;
            AddTimeline(row.Complaint, "CITIZEN_VERIFICATION_REJECTED",
                "Supervisor found the officer evidence sufficient and closed the complaint: " + request.Remarks.Trim());
        }

        await _db.SaveChangesAsync(ct);
        return new
        {
            verification = Map(row),
            supervisorDecision = dispute.SupervisorDecision,
            supervisorRemarks = dispute.SupervisorRemarks,
            reviewedAt = dispute.ReviewedAt,
            appealDeadline = dispute.AppealDeadline
        };
    }

    private IQueryable<ComplaintVerification> QueryQueue(bool track = false)
    {
        var query = _db.ComplaintVerifications
            .Include(x => x.Complaint)
                .ThenInclude(x => x.Department)
            .Include(x => x.Complaint)
                .ThenInclude(x => x.Ward)
            .Include(x => x.Complaint)
                .ThenInclude(x => x.Images);

        return track ? query.AsTracking() : query.AsNoTracking();
    }

    private async Task<ComplaintVerification> LoadCitizenAsync(long id, CancellationToken ct)
    {
        EnsureRole("Citizen");
        await EnsureRowsAsync(ct);
        return await QueryQueue(true)
            .SingleOrDefaultAsync(x => x.ComplaintId == id && x.CitizenId == RequireUser(), ct)
            ?? throw new NotFoundException("Verification was not found.");
    }

    private async Task ApplyCitizenDecisionAsync(ComplaintVerification row, VerifyComplaintRequest request, CancellationToken ct)
    {
        var decision = ResolveDecision(request);
        var distance = CalculateDistance(
            (double)row.Complaint.Latitude,
            (double)row.Complaint.Longitude,
            (double)request.Latitude,
            (double)request.Longitude);

        if (distance > AllowedDistanceMetres)
            throw new BusinessRuleViolationException(
                $"Move within {AllowedDistanceMetres:0} metres of the complaint. Current distance: {Math.Round(distance)} metres.");

        var now = DateTimeOffset.UtcNow;
        row.Decision = decision;
        row.Rating = request.Rating;
        row.Remarks = request.Remarks?.Trim();
        row.SubmittedLatitude = request.Latitude;
        row.SubmittedLongitude = request.Longitude;
        row.DistanceMetres = distance;
        row.CompletedAt = now;

        if (decision == VerificationDecision.Approved)
        {
            row.Complaint.Status = ComplaintStatus.Closed;
            row.Complaint.ClosedAt = now;
            AddTimeline(row.Complaint, "CITIZEN_VERIFIED",
                $"Citizen approved the resolution with rating {request.Rating}/5.");
            return;
        }

        row.Complaint.Status = ComplaintStatus.Disputed;
        row.Complaint.ClosedAt = null;
        var cycle = await _db.DisputeAuditLogs.CountAsync(x => x.ComplaintId == row.ComplaintId, ct) + 1;
        var remarks = string.IsNullOrWhiteSpace(request.Remarks)
            ? DefaultDecisionRemarks(decision)
            : request.Remarks.Trim();

        _db.DisputeAuditLogs.Add(new DisputeAuditLog
        {
            ComplaintId = row.ComplaintId,
            RaisedByUserId = RequireUser(),
            Status = DisputeStatus.UnderSupervisorReview,
            CycleNumber = cycle,
            CitizenRemarks = $"{DecisionLabel(decision)}: {remarks}",
            RaisedAt = now
        });

        AddTimeline(row.Complaint, DecisionEvent(decision),
            $"Citizen selected '{DecisionLabel(decision)}' and requested supervisor review: {remarks}");
    }

    private async Task EnsureCitizenCanStillActAsync(ComplaintVerification row, CancellationToken ct)
    {
        if (row.Decision == VerificationDecision.Pending && row.Complaint.Status == ComplaintStatus.VerificationPending)
            return;

        if (row.Complaint.Status == ComplaintStatus.Disputed)
        {
            var pendingReview = await _db.DisputeAuditLogs.AsNoTracking().AnyAsync(x =>
                x.ComplaintId == row.ComplaintId &&
                (x.Status == DisputeStatus.Raised || x.Status == DisputeStatus.UnderSupervisorReview), ct);
            if (pendingReview) return;
        }

        throw new BusinessRuleViolationException("Verification evidence can no longer be changed after official review.");
    }

    private async Task<DisputeAuditLog> RequirePendingCitizenReviewAsync(long complaintId, CancellationToken ct) =>
        await _db.DisputeAuditLogs
            .Where(x => x.ComplaintId == complaintId &&
                        (x.Status == DisputeStatus.Raised || x.Status == DisputeStatus.UnderSupervisorReview))
            .OrderByDescending(x => x.CycleNumber)
            .ThenByDescending(x => x.RaisedAt)
            .FirstOrDefaultAsync(ct)
        ?? throw new BusinessRuleViolationException("Only a decision awaiting supervisor review can be amended or withdrawn.");

    private void ClosePendingCitizenReview(DisputeAuditLog dispute, string decision, string remarks)
    {
        var now = DateTimeOffset.UtcNow;
        dispute.Status = DisputeStatus.Closed;
        dispute.ReviewedByUserId = RequireUser();
        dispute.ReviewedAt = now;
        dispute.ResolvedAt = now;
        dispute.SupervisorDecision = decision;
        dispute.SupervisorRemarks = remarks;
        dispute.AppealDeadline = null;
    }

    private static void ResetForCitizenDecision(ComplaintVerification row)
    {
        row.Decision = VerificationDecision.Pending;
        row.Rating = null;
        row.Remarks = null;
        row.SubmittedLatitude = null;
        row.SubmittedLongitude = null;
        row.DistanceMetres = null;
        row.CompletedAt = null;
        row.Complaint.Status = ComplaintStatus.VerificationPending;
        row.Complaint.ClosedAt = null;
    }

    private async Task EnsureRowsAsync(CancellationToken ct)
    {
        var existing = await _db.ComplaintVerifications.Select(x => x.ComplaintId).ToListAsync(ct);
        var items = await _db.Complaints
            .Where(x => x.Status == ComplaintStatus.VerificationPending && !existing.Contains(x.Id))
            .ToListAsync(ct);

        foreach (var complaint in items)
        {
            _db.ComplaintVerifications.Add(new ComplaintVerification
            {
                ComplaintId = complaint.Id,
                CitizenId = complaint.CitizenId,
                DueAt = (complaint.ResolvedAt ?? DateTimeOffset.UtcNow).AddHours(Window(complaint.Priority))
            });
        }

        if (items.Count > 0) await _db.SaveChangesAsync(ct);
    }

    private static int Window(ComplaintPriority priority) => priority switch
    {
        ComplaintPriority.Critical => 24,
        ComplaintPriority.High => 48,
        _ => 72
    };

    private IQueryable<ComplaintVerification> ApplyScope(IQueryable<ComplaintVerification> query)
    {
        if (!string.Equals(_current.Role, "Supervisor", StringComparison.OrdinalIgnoreCase)) return query;
        if (!_current.DepartmentId.HasValue)
            throw new BusinessRuleViolationException("Supervisor department scope is missing.");

        query = query.Where(x => x.Complaint.DepartmentId == _current.DepartmentId.Value);
        if (_current.WardId.HasValue)
            query = query.Where(x => x.Complaint.WardId == _current.WardId.Value);
        return query;
    }

    private void EnsureCanView(ComplaintVerification row)
    {
        if (string.Equals(_current.Role, "Citizen", StringComparison.OrdinalIgnoreCase))
        {
            if (row.CitizenId != RequireUser())
                throw new NotFoundException("Verification was not found.");
            return;
        }

        if (string.Equals(_current.Role, "Officer", StringComparison.OrdinalIgnoreCase))
        {
            if (row.Complaint.AssignedOfficerId != RequireUser())
                throw new NotFoundException("Verification was not found.");
            return;
        }

        if (string.Equals(_current.Role, "Supervisor", StringComparison.OrdinalIgnoreCase))
        {
            if (row.Complaint.DepartmentId != _current.DepartmentId ||
                (_current.WardId.HasValue && row.Complaint.WardId != _current.WardId))
                throw new NotFoundException("Verification was not found.");
            return;
        }

        if (!new[] { "Admin", "SuperAdmin" }.Contains(_current.Role, StringComparer.OrdinalIgnoreCase))
            throw new BusinessRuleViolationException("Verification access is not available for this role.");
    }

    private void EnsureHistoryRole()
    {
        if (!new[] { "Citizen", "Supervisor", "Admin", "SuperAdmin" }.Contains(_current.Role, StringComparer.OrdinalIgnoreCase))
            throw new BusinessRuleViolationException("Verification-history access is not available for this role.");
    }

    private void EnsureSupervisor()
    {
        if (!new[] { "Supervisor", "Admin", "SuperAdmin" }.Contains(_current.Role, StringComparer.OrdinalIgnoreCase))
            throw new BusinessRuleViolationException("Supervisor access is required.");
    }

    private bool IsReminderRole() =>
        new[] { "Officer", "Supervisor", "Admin", "SuperAdmin" }
            .Contains(_current.Role, StringComparer.OrdinalIgnoreCase);

    private void EnsureReminderRole()
    {
        if (!IsReminderRole())
            throw new BusinessRuleViolationException("The assigned Officer or authorized supervisory staff is required.");
    }

    private void EnsureRole(string role)
    {
        if (!string.Equals(_current.Role, role, StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleViolationException(role + " access is required.");
    }

    private long RequireUser() =>
        _current.UserId ?? throw new BusinessRuleViolationException("Authenticated user is required.");

    private void AddTimeline(Complaint complaint, string eventType, string description) =>
        complaint.Timeline.Add(new ComplaintTimeline
        {
            ComplaintId = complaint.Id,
            UserId = _current.UserId,
            EventType = eventType,
            Description = description,
            Timestamp = DateTimeOffset.UtcNow
        });

    private async Task<Dictionary<long, VerificationReminderPolicyDecision>> GetReminderStatesAsync(
        IReadOnlyCollection<ComplaintVerification> rows,
        CancellationToken ct)
    {
        if (rows.Count == 0) return new Dictionary<long, VerificationReminderPolicyDecision>();

        var complaintIds = rows.Select(x => x.ComplaintId).Distinct().ToArray();
        var events = await _db.ComplaintTimelines.AsNoTracking()
            .Where(x => complaintIds.Contains(x.ComplaintId) && ReminderEvents.Contains(x.EventType))
            .Select(x => new ReminderEventSnapshot
            {
                ComplaintId = x.ComplaintId,
                EventType = x.EventType,
                Timestamp = x.Timestamp
            })
            .ToListAsync(ct);

        var grouped = events
            .GroupBy(x => x.ComplaintId)
            .ToDictionary(x => x.Key, x => x.ToArray());
        var result = new Dictionary<long, VerificationReminderPolicyDecision>();

        foreach (var row in rows)
        {
            grouped.TryGetValue(row.ComplaintId, out var complaintEvents);
            complaintEvents ??= [];
            result[row.ComplaintId] = BuildReminderState(row, complaintEvents);
        }

        return result;
    }

    private async Task<VerificationReminderPolicyDecision> GetReminderStateAsync(
        ComplaintVerification row,
        CancellationToken ct)
    {
        var events = await _db.ComplaintTimelines.AsNoTracking()
            .Where(x => x.ComplaintId == row.ComplaintId && ReminderEvents.Contains(x.EventType))
            .Select(x => new ReminderEventSnapshot
            {
                ComplaintId = x.ComplaintId,
                EventType = x.EventType,
                Timestamp = x.Timestamp
            })
            .ToListAsync(ct);
        return BuildReminderState(row, events);
    }

    private VerificationReminderPolicyDecision BuildReminderState(
        ComplaintVerification row,
        IReadOnlyCollection<ReminderEventSnapshot> events)
    {
        var isOfficer = string.Equals(_current.Role, "Officer", StringComparison.OrdinalIgnoreCase);
        var actorCount = events.Count(x => isOfficer
            ? string.Equals(x.EventType, OfficerReminderEvent, StringComparison.OrdinalIgnoreCase)
            : string.Equals(x.EventType, StaffReminderEvent, StringComparison.OrdinalIgnoreCase) ||
              string.Equals(x.EventType, LegacyReminderEvent, StringComparison.OrdinalIgnoreCase));

        var lastEventAt = events.Count == 0
            ? (DateTimeOffset?)null
            : events.Max(x => x.Timestamp);
        var lastReminderAt = Latest(row.ReminderSentAt, lastEventAt);

        if (row.Decision != VerificationDecision.Pending || row.Complaint.Status != ComplaintStatus.VerificationPending)
        {
            var policy = VerificationReminderPolicy.Evaluate(_current.Role, actorCount, lastReminderAt, DateTimeOffset.UtcNow);
            return policy with
            {
                Allowed = false,
                UnavailableReason = "The Citizen verification decision is no longer pending."
            };
        }

        return VerificationReminderPolicy.Evaluate(
            _current.Role,
            actorCount,
            lastReminderAt,
            DateTimeOffset.UtcNow);
    }

    private static DateTimeOffset? Latest(DateTimeOffset? left, DateTimeOffset? right)
    {
        if (!left.HasValue) return right;
        if (!right.HasValue) return left;
        return left.Value >= right.Value ? left : right;
    }

    private void AddReminderAudit(
        ComplaintVerification row,
        VerificationReminderPolicyDecision before,
        DateTimeOffset now)
    {
        var context = _httpContextAccessor.HttpContext;
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _current.UserId,
            UserEmail = _current.Email,
            UserRole = _current.Role,
            Action = string.Equals(_current.Role, "Officer", StringComparison.OrdinalIgnoreCase)
                ? "OFFICER_VERIFICATION_REMINDER_SENT"
                : "VERIFICATION_REMINDER_SENT",
            EntityName = nameof(ComplaintVerification),
            EntityId = row.Id.ToString(),
            OldValuesJson = JsonSerializer.Serialize(new
            {
                row.ComplaintId,
                reminderCount = before.SentCount,
                before.LastReminderAt
            }),
            NewValuesJson = JsonSerializer.Serialize(new
            {
                row.ComplaintId,
                reminderCount = before.SentCount + 1,
                reminderSentAt = now,
                dueAt = row.DueAt
            }),
            IpAddress = context?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = context?.Request.Headers.UserAgent.ToString(),
            CorrelationId = context?.TraceIdentifier,
            Severity = "Information",
            Success = true,
            HttpStatusCode = 200,
            CreatedAt = now
        });
    }

    private sealed class ReminderEventSnapshot
    {
        public long ComplaintId { get; init; }
        public string EventType { get; init; } = string.Empty;
        public DateTimeOffset Timestamp { get; init; }
    }

    private static VerificationQueueItem QueueItem(
        ComplaintVerification row,
        bool requiresSupervisorDecision,
        VerificationReminderPolicyDecision? reminderState)
    {
        var remaining = (long)(row.DueAt - DateTimeOffset.UtcNow).TotalMinutes;
        return new VerificationQueueItem(
            row.ComplaintId,
            Reference(row.ComplaintId),
            row.Complaint.Title,
            row.Complaint.Priority.ToString(),
            row.Complaint.Department.Name,
            row.Complaint.Ward.Name,
            row.DueAt,
            remaining,
            row.Decision == VerificationDecision.Pending && row.DueAt < DateTimeOffset.UtcNow,
            row.Decision.ToString(),
            requiresSupervisorDecision,
            reminderState?.Allowed == true && !requiresSupervisorDecision,
            reminderState?.SentCount ?? 0,
            reminderState?.MaximumAllowed ?? 0,
            reminderState?.LastReminderAt,
            reminderState?.NextAllowedAt,
            requiresSupervisorDecision
                ? "A reminder is unavailable while a Supervisor decision is pending."
                : reminderState?.UnavailableReason);
    }

    private static VerificationResponse Map(
        ComplaintVerification row,
        VerificationReminderPolicyDecision? reminderState = null)
    {
        var pendingSupervisorReview = row.Complaint.Status == ComplaintStatus.Disputed &&
                                      row.Decision is not VerificationDecision.Pending and not VerificationDecision.Approved;
        var citizenEvidence = row.Complaint.Images
            .Where(IsCitizenVerificationEvidence)
            .OrderBy(x => x.UploadedAt)
            .Select(x => new VerificationEvidenceItem(
                x.Id,
                x.FileName,
                x.FileSize,
                x.MimeType,
                x.UploadedAt,
                $"/complaints/{row.ComplaintId}/images/{x.Id}"))
            .ToArray();

        return new VerificationResponse(
            row.ComplaintId,
            Reference(row.ComplaintId),
            row.Complaint.Title,
            row.Complaint.Status.ToString(),
            row.Decision.ToString(),
            row.Rating,
            row.Remarks,
            row.DistanceMetres,
            row.DueAt,
            row.CompletedAt,
            row.Decision == VerificationDecision.Pending && row.Complaint.Status == ComplaintStatus.VerificationPending,
            pendingSupervisorReview,
            pendingSupervisorReview,
            reminderState?.Allowed == true,
            reminderState?.SentCount ?? 0,
            reminderState?.MaximumAllowed ?? 0,
            reminderState?.LastReminderAt,
            reminderState?.NextAllowedAt,
            reminderState?.UnavailableReason,
            citizenEvidence);
    }

    private static VerificationDecision ResolveDecision(VerifyComplaintRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Decision) && request.Approved.HasValue)
            return request.Approved.Value ? VerificationDecision.Approved : VerificationDecision.NotResolvedYet;

        var normalized = new string((request.Decision ?? string.Empty).Where(char.IsLetterOrDigit).ToArray());
        if (normalized.Equals("Approved", StringComparison.OrdinalIgnoreCase)) return VerificationDecision.Approved;
        if (normalized.Equals("Rejected", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("NotResolvedYet", StringComparison.OrdinalIgnoreCase)) return VerificationDecision.NotResolvedYet;
        if (normalized.Equals("RequestRevisit", StringComparison.OrdinalIgnoreCase)) return VerificationDecision.RequestRevisit;
        if (normalized.Equals("PartiallyResolved", StringComparison.OrdinalIgnoreCase)) return VerificationDecision.PartiallyResolved;

        throw new CivicHero.Backend.Core.Exceptions.ValidationException([
            "Decision must be Approved, NotResolvedYet, RequestRevisit, or PartiallyResolved."
        ]);
    }

    private static string DecisionLabel(VerificationDecision decision) => decision switch
    {
        VerificationDecision.NotResolvedYet => "Not Resolved Yet",
        VerificationDecision.RequestRevisit => "Request Revisit",
        VerificationDecision.PartiallyResolved => "Partially Resolved",
        _ => decision.ToString()
    };

    private static string DecisionEvent(VerificationDecision decision) => decision switch
    {
        VerificationDecision.NotResolvedYet => "RESOLUTION_NOT_RESOLVED",
        VerificationDecision.RequestRevisit => "RESOLUTION_REVISIT_REQUESTED",
        VerificationDecision.PartiallyResolved => "RESOLUTION_PARTIALLY_RESOLVED",
        _ => "RESOLUTION_REJECTED"
    };

    private static string CitizenDecisionFromRemarks(string remarks)
    {
        var separator = remarks.IndexOf(':');
        if (separator <= 0) return "LegacyDecision";
        return remarks[..separator].Trim();
    }

    private static string DefaultDecisionRemarks(VerificationDecision decision) => decision switch
    {
        VerificationDecision.NotResolvedYet => "Citizen reported that the issue is not resolved.",
        VerificationDecision.RequestRevisit => "Citizen requested another official site visit.",
        VerificationDecision.PartiallyResolved => "Citizen reported that the issue is only partially resolved.",
        _ => "Citizen requested supervisor review."
    };

    private static bool IsCitizenVerificationEvidence(ComplaintImage image) =>
        image.S3Key.StartsWith("verification-evidence/", StringComparison.OrdinalIgnoreCase);

    private static string Reference(long complaintId) => "CH-" + complaintId.ToString("D6");

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadius = 6_371_000;
        var p1 = lat1 * Math.PI / 180;
        var p2 = lat2 * Math.PI / 180;
        var deltaLat = (lat2 - lat1) * Math.PI / 180;
        var deltaLon = (lon2 - lon1) * Math.PI / 180;
        var h = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(p1) * Math.Cos(p2) * Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
        return earthRadius * 2 * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));
    }

    private static async Task<string> ValidateEvidenceAsync(IFormFile file, CancellationToken ct)
    {
        if (file.Length <= 0 || file.Length > MaximumEvidenceBytes)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException([
                $"Each verification image must be between 1 byte and {MaximumEvidenceBytes / 1024 / 1024} MB."
            ]);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var contentType = file.ContentType.ToLowerInvariant();
        var validPair = (contentType, extension) switch
        {
            ("image/jpeg", ".jpg" or ".jpeg") => true,
            ("image/png", ".png") => true,
            ("image/webp", ".webp") => true,
            _ => false
        };

        if (!validPair)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException([
                "Verification evidence must be a JPEG, PNG, or WebP image with a matching file extension."
            ]);

        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(header.AsMemory(0, header.Length), ct);
        if (!HasExpectedImageSignature(header.AsSpan(0, bytesRead), contentType))
            throw new CivicHero.Backend.Core.Exceptions.ValidationException([
                "The uploaded verification image content does not match its declared file type."
            ]);

        return extension == ".jpeg" ? ".jpg" : extension;
    }

    private static bool HasExpectedImageSignature(ReadOnlySpan<byte> bytes, string contentType) => contentType switch
    {
        "image/jpeg" => bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
        "image/png" => bytes.Length >= 8 &&
                       bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
                       bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A,
        "image/webp" => bytes.Length >= 12 &&
                        bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F' &&
                        bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P',
        _ => false
    };
}
