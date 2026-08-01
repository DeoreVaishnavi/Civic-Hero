using System.Text.Json;
using CivicHero.Backend.Core.DTOs.Ai;
using CivicHero.Backend.Core.DTOs.Notifications;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.AI;
using CivicHero.Backend.Infrastructure.Configurations;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Core.Services;

public sealed class AiTriageService : IAiTriageService
{
    private const string EscalatedToAdminDecision = "EscalatedToAdmin";

    private readonly CivicDbContext _db;
    private readonly GeminiAiService _gemini;
    private readonly RuleBasedAiService _rules;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;
    private readonly AiOptions _options;

    public AiTriageService(
        CivicDbContext db,
        GeminiAiService gemini,
        RuleBasedAiService rules,
        ICurrentUserService currentUser,
        INotificationService notifications,
        IOptions<AiOptions> options)
    {
        _db = db;
        _gemini = gemini;
        _rules = rules;
        _currentUser = currentUser;
        _notifications = notifications;
        _options = options.Value;
    }

    public async Task<ClassificationResponse> ClassifyAsync(AiTextRequest request, CancellationToken cancellationToken = default)
    {
        var result = await ProviderAnalysisAsync(request, cancellationToken);
        var department = await FindDepartmentAsync(result.Category, cancellationToken);
        return new ClassificationResponse(result.Category, department?.Id, department?.Name, result.ClassificationConfidence, result.Provider, result.Reasoning);
    }

    public async Task<DuplicateCheckResponse> CheckDuplicateAsync(AiTextRequest request, long? excludeComplaintId = null, CancellationToken cancellationToken = default)
    {
        if (!request.Latitude.HasValue || !request.Longitude.HasValue)
            return new DuplicateCheckResponse("LocationRequired", 0m, null, [], "Latitude and longitude are required for duplicate detection.");

        var radiusKm = Math.Clamp(_options.DuplicateRadiusMeters, 100, 2000) / 1000d;
        var latitudeDelta = radiusKm / 111d;
        var longitudeFactor = Math.Max(0.2d, Math.Cos((double)request.Latitude.Value * Math.PI / 180d));
        var longitudeDelta = radiusKm / (111d * longitudeFactor);
        var minLat = request.Latitude.Value - (decimal)latitudeDelta;
        var maxLat = request.Latitude.Value + (decimal)latitudeDelta;
        var minLon = request.Longitude.Value - (decimal)longitudeDelta;
        var maxLon = request.Longitude.Value + (decimal)longitudeDelta;

        var candidates = await _db.Complaints.AsNoTracking()
            .Where(entity => (!excludeComplaintId.HasValue || entity.Id != excludeComplaintId.Value)
                && entity.Status != ComplaintStatus.Withdrawn
                && entity.Status != ComplaintStatus.ClosedFraud
                && entity.Latitude >= minLat && entity.Latitude <= maxLat
                && entity.Longitude >= minLon && entity.Longitude <= maxLon)
            .OrderByDescending(entity => entity.CreatedAt)
            .Take(100)
            .Select(entity => new { entity.Id, entity.Title, entity.Description, entity.Latitude, entity.Longitude })
            .ToListAsync(cancellationToken);

        var inputText = $"{request.Title} {request.Description}";
        var matches = candidates.Select(entity =>
        {
            var distance = HaversineMeters(request.Latitude.Value, request.Longitude.Value, entity.Latitude, entity.Longitude);
            var textScore = TextSimilarity(inputText, $"{entity.Title} {entity.Description}");
            var distanceScore = Math.Max(0m, 1m - ((decimal)distance / Math.Max(1, _options.DuplicateRadiusMeters)));
            var combined = Math.Clamp((textScore * 0.72m) + (distanceScore * 0.28m), 0m, 1m);
            return new DuplicateMatchResponse(entity.Id, entity.Title, Math.Round((decimal)distance, 1), Math.Round(textScore, 4), Math.Round(combined, 4));
        }).Where(item => item.DistanceMeters <= _options.DuplicateRadiusMeters)
          .OrderByDescending(item => item.CombinedScore).Take(5).ToList();

        var best = matches.FirstOrDefault();
        var score = best?.CombinedScore ?? 0m;
        var status = score >= _options.ConfirmedDuplicateThreshold ? "ConfirmedDuplicate"
            : score >= _options.PossibleDuplicateThreshold ? "PossibleDuplicate" : "Unique";
        var reasoning = best is null ? "No nearby complaint candidates were found."
            : $"Best match is complaint #{best.ComplaintId} at {best.DistanceMeters:0} metres with {best.TextSimilarity:P0} text similarity.";
        return new DuplicateCheckResponse(status, score, best?.ComplaintId, matches, reasoning);
    }

    public async Task<FraudCheckResponse> CheckFraudAsync(AiTextRequest request, CancellationToken cancellationToken = default)
    {
        var result = await ProviderAnalysisAsync(request, cancellationToken);
        return await BuildFraudResponseAsync(request, result, cancellationToken);
    }

    public async Task<PriorityPredictionResponse> PredictPriorityAsync(AiTextRequest request, CancellationToken cancellationToken = default)
    {
        var result = await ProviderAnalysisAsync(request, cancellationToken);
        return new PriorityPredictionResponse(result.Priority.ToString(), result.PriorityScore, [result.Reasoning], result.Provider);
    }

    public async Task<AiTriageResponse> AnalyzeComplaintAsync(long complaintId, bool force = false, CancellationToken cancellationToken = default)
    {
        var complaint = await _db.Complaints.Include(entity => entity.Citizen).FirstOrDefaultAsync(entity => entity.Id == complaintId, cancellationToken)
            ?? throw new NotFoundException("Complaint was not found.");
        if (_currentUser.IsAuthenticated) EnsureComplaintReviewScope(complaint);
        if (!force && complaint.AiTriagedAt.HasValue)
        {
            var latest = await _db.AiTriageAnalyses.AsNoTracking().Where(entity => entity.ComplaintId == complaintId)
                .OrderByDescending(entity => entity.AnalyzedAt).FirstOrDefaultAsync(cancellationToken);
            if (latest is not null) return Map(complaint, latest);
        }

        complaint.Status = ComplaintStatus.AiTriage;

        var input = new AiTextRequest(complaint.Title, complaint.Description, complaint.Category, complaint.Latitude, complaint.Longitude, complaint.CitizenId);
        var provider = await ProviderAnalysisAsync(input, cancellationToken);
        var department = await FindDepartmentAsync(provider.Category, cancellationToken);
        var classification = new ClassificationResponse(provider.Category, department?.Id, department?.Name, provider.ClassificationConfidence, provider.Provider, provider.Reasoning);
        var duplicate = await CheckDuplicateAsync(input, complaint.Id, cancellationToken);
        var fraud = await BuildFraudResponseAsync(input, provider, cancellationToken);
        var priority = new PriorityPredictionResponse(provider.Priority.ToString(), provider.PriorityScore, [provider.Reasoning], provider.Provider);
        var requiresReview = duplicate.Status == "PossibleDuplicate" || fraud.RequiresManualReview || classification.Confidence < 0.60m;

        var analysis = new AiTriageAnalysis
        {
            ComplaintId = complaint.Id,
            Provider = provider.Provider,
            Model = provider.Model,
            PredictedCategory = classification.Category,
            PredictedDepartmentId = classification.DepartmentId,
            ClassificationConfidence = classification.Confidence,
            DuplicateStatus = duplicate.Status,
            DuplicateComplaintId = duplicate.MatchingComplaintId,
            DuplicateScore = duplicate.Score,
            FraudVerdict = fraud.Verdict,
            FraudScore = fraud.Score,
            PredictedPriority = provider.Priority,
            PriorityScore = provider.PriorityScore,
            RequiresManualReview = requiresReview,
            Reasoning = $"{classification.Reasoning} {duplicate.Reasoning} {string.Join(" ", fraud.Signals)}".Trim(),
            RawProviderResponse = provider.RawResponse,
            AnalyzedAt = DateTimeOffset.UtcNow
        };
        _db.AiTriageAnalyses.Add(analysis);
        _db.AiFraudAnalyses.Add(new AiFraudAnalysis
        {
            ComplaintId = complaint.Id,
            FraudScore = (float)fraud.Score,
            Verdict = fraud.Verdict,
            Reasoning = string.Join(" ", fraud.Signals),
            Provider = provider.Provider,
            Model = provider.Model,
            RequiresManualReview = fraud.RequiresManualReview,
            AnalyzedAt = DateTimeOffset.UtcNow
        });

        complaint.AiTriagedAt = analysis.AnalyzedAt;
        complaint.AiSuggestedCategory = classification.Category;
        complaint.AiConfidence = classification.Confidence;
        complaint.AiRiskScore = fraud.Score;
        complaint.Priority = provider.Priority;
        complaint.DuplicateOfComplaintId = duplicate.Status == "ConfirmedDuplicate" ? duplicate.MatchingComplaintId : null;
        complaint.Status = duplicate.Status == "ConfirmedDuplicate" ? ComplaintStatus.Merged
            : fraud.Score >= _options.FraudReviewThreshold ? ComplaintStatus.FraudReview
            : ComplaintStatus.Created;
        complaint.Timeline.Add(new ComplaintTimeline
        {
            EventType = "AI_TRIAGE_COMPLETED",
            Description = $"AI suggested {classification.Category}, {provider.Priority} priority, duplicate status {duplicate.Status}, fraud verdict {fraud.Verdict}.",
            Timestamp = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        return Map(complaint, analysis);
    }

    public async Task<IReadOnlyList<AiReviewQueueItem>> GetReviewQueueAsync(CancellationToken cancellationToken = default)
    {
        EnsureReviewRole();
        IQueryable<AiTriageAnalysis> source = _db.AiTriageAnalyses.AsNoTracking()
            .Include(entity => entity.Complaint).ThenInclude(entity => entity.Citizen)
            .Include(entity => entity.Complaint).ThenInclude(entity => entity.Department)
            .Include(entity => entity.Complaint).ThenInclude(entity => entity.Ward)
            .Where(entity => entity.RequiresManualReview && entity.ReviewedAt == null);

        source = ApplyReviewScope(source);
        if (IsSupervisor())
            source = source.Where(entity => entity.ReviewDecision == null || entity.ReviewDecision != EscalatedToAdminDecision);

        var rows = await source.OrderByDescending(entity => entity.AnalyzedAt)
            .Take(250)
            .ToListAsync(cancellationToken);
        var latestRows = rows.GroupBy(entity => entity.ComplaintId)
            .Select(group => group.OrderByDescending(entity => entity.AnalyzedAt).First())
            .ToList();
        var predictedDepartmentIds = latestRows.Where(entity => entity.PredictedDepartmentId.HasValue)
            .Select(entity => entity.PredictedDepartmentId!.Value)
            .Distinct()
            .ToArray();
        var predictedDepartments = predictedDepartmentIds.Length == 0
            ? new Dictionary<long, string>()
            : await _db.Departments.AsNoTracking()
                .Where(entity => predictedDepartmentIds.Contains(entity.Id))
                .ToDictionaryAsync(entity => entity.Id, entity => entity.Name, cancellationToken);

        return latestRows.Select(entity => new AiReviewQueueItem(
            entity.ComplaintId,
            entity.Complaint.Title,
            entity.Complaint.Citizen.FullName,
            entity.Complaint.Status.ToString(),
            entity.Complaint.DepartmentId,
            entity.Complaint.Department.Name,
            entity.Complaint.WardId,
            entity.Complaint.Ward.Name,
            entity.Complaint.Category,
            entity.PredictedCategory,
            entity.PredictedDepartmentId,
            entity.PredictedDepartmentId.HasValue && predictedDepartments.TryGetValue(entity.PredictedDepartmentId.Value, out var predictedName) ? predictedName : null,
            entity.PredictedPriority.ToString(),
            entity.DuplicateStatus,
            entity.DuplicateComplaintId,
            entity.DuplicateScore,
            entity.FraudVerdict,
            entity.FraudScore,
            entity.Reasoning,
            string.Equals(entity.ReviewDecision, EscalatedToAdminDecision, StringComparison.OrdinalIgnoreCase),
            string.Equals(entity.ReviewDecision, EscalatedToAdminDecision, StringComparison.OrdinalIgnoreCase) ? entity.ReviewNotes : null,
            entity.AnalyzedAt)).ToList();
    }

    public async Task<AiTriageResponse> DecideAsync(long complaintId, AiReviewDecisionRequest request, CancellationToken cancellationToken = default)
    {
        EnsureReviewRole();
        var complaint = await _db.Complaints
            .FirstOrDefaultAsync(entity => entity.Id == complaintId, cancellationToken)
            ?? throw new NotFoundException("Complaint was not found.");
        EnsureComplaintReviewScope(complaint);

        var pendingAnalyses = await _db.AiTriageAnalyses
            .Where(entity => entity.ComplaintId == complaintId && entity.ReviewedAt == null && entity.RequiresManualReview)
            .OrderByDescending(entity => entity.AnalyzedAt)
            .ToListAsync(cancellationToken);
        var analysis = pendingAnalyses.FirstOrDefault()
            ?? throw new NotFoundException("No pending AI review was found for this complaint.");
        if (IsSupervisor() && string.Equals(analysis.ReviewDecision, EscalatedToAdminDecision, StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleViolationException("This AI review has already been escalated to an administrator.");

        var decision = (request.Decision ?? string.Empty).Trim();
        var notes = request.Notes?.Trim();
        EnsureDecisionAllowed(decision);
        if (decision.Equals("Escalate", StringComparison.OrdinalIgnoreCase))
        {
            RequireDetailedNotes(notes, "Escalation notes");
            MarkSupersededAnalyses(pendingAnalyses.Skip(1));
            analysis.ReviewDecision = EscalatedToAdminDecision;
            analysis.ReviewNotes = notes;
            analysis.ReviewedByUserId = RequireCurrentUserId();
            analysis.ReviewedAt = null;
            analysis.RequiresManualReview = true;
            complaint.Status = ComplaintStatus.FraudReview;
            complaint.Timeline.Add(new ComplaintTimeline
            {
                UserId = _currentUser.UserId,
                EventType = "AI_REVIEW_ESCALATED",
                Description = $"Supervisor escalated the AI review to Admin. Reason: {notes}",
                Timestamp = DateTimeOffset.UtcNow
            });
            AddReviewAudit("AI_REVIEW_ESCALATED_TO_ADMIN", complaint, analysis, new { decision = EscalatedToAdminDecision, notes });
            await _db.SaveChangesAsync(cancellationToken);
            await NotifyAdministratorsAsync(complaint, notes!, cancellationToken);
            return Map(complaint, analysis);
        }

        var previous = new
        {
            status = complaint.Status.ToString(),
            complaint.Category,
            complaint.DepartmentId,
            complaint.WardId,
            complaint.DuplicateOfComplaintId,
            analysis.ReviewDecision,
            analysis.ReviewNotes
        };
        string? routingSummary = null;

        if (decision.Equals("Clear", StringComparison.OrdinalIgnoreCase))
        {
            complaint.Status = ComplaintStatus.Created;
            complaint.DuplicateOfComplaintId = null;
            complaint.ClosedAt = null;
        }
        else if (decision.Equals("RejectInvalid", StringComparison.OrdinalIgnoreCase))
        {
            RequireDetailedNotes(notes, "Rejection reason");
            complaint.Status = ComplaintStatus.ClosedFraud;
            complaint.ClosedAt = DateTimeOffset.UtcNow;
        }
        else if (decision.Equals("ConfirmFraud", StringComparison.OrdinalIgnoreCase))
        {
            complaint.Status = ComplaintStatus.ClosedFraud;
            complaint.ClosedAt = DateTimeOffset.UtcNow;
        }
        else if (decision.Equals("Merge", StringComparison.OrdinalIgnoreCase))
        {
            if (IsSupervisor()) RequireDetailedNotes(notes, "Merge reason");
            if (!request.MergeIntoComplaintId.HasValue || request.MergeIntoComplaintId.Value == complaintId)
                throw new ValidationException(["A different parent complaint ID is required for merge."]);
            var parent = await _db.Complaints.AsNoTracking()
                .FirstOrDefaultAsync(entity => entity.Id == request.MergeIntoComplaintId.Value, cancellationToken)
                ?? throw new NotFoundException("The parent complaint was not found.");
            if (IsSupervisor()) EnsureComplaintReviewScope(parent);
            complaint.Status = ComplaintStatus.Merged;
            complaint.DuplicateOfComplaintId = request.MergeIntoComplaintId.Value;
        }
        else if (decision.Equals("OverrideRouting", StringComparison.OrdinalIgnoreCase))
        {
            RequireDetailedNotes(notes, "Routing override reason");
            routingSummary = await ApplyRoutingOverrideAsync(complaint, request, notes!, cancellationToken);
        }
        else if (!decision.Equals("Reanalyze", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException(["Decision must be Clear, OverrideRouting, RejectInvalid, Escalate, Merge, ConfirmFraud, or Reanalyze."]);
        }

        MarkSupersededAnalyses(pendingAnalyses.Skip(1));
        analysis.ReviewDecision = decision;
        analysis.ReviewNotes = notes;
        analysis.ReviewedByUserId = RequireCurrentUserId();
        analysis.ReviewedAt = DateTimeOffset.UtcNow;
        analysis.RequiresManualReview = false;

        var description = routingSummary is null
            ? $"Manual AI review decision: {decision}. {notes}".Trim()
            : $"Manual AI review decision: {decision}. {routingSummary} Reason: {notes}";
        complaint.Timeline.Add(new ComplaintTimeline
        {
            UserId = _currentUser.UserId,
            EventType = "AI_REVIEW_DECISION",
            Description = description,
            Timestamp = DateTimeOffset.UtcNow
        });
        AddReviewAudit("AI_REVIEW_DECISION_RECORDED", complaint, analysis, new
        {
            decision,
            notes,
            request.MergeIntoComplaintId,
            request.Category,
            request.DepartmentId,
            request.WardId,
            status = complaint.Status.ToString(),
            complaint.DuplicateOfComplaintId
        }, previous);
        await _db.SaveChangesAsync(cancellationToken);

        if (decision.Equals("Reanalyze", StringComparison.OrdinalIgnoreCase))
            return await AnalyzeComplaintAsync(complaintId, true, cancellationToken);

        await NotifyCitizenAboutDecisionAsync(complaint, decision, notes, cancellationToken);
        return Map(complaint, analysis);
    }

    public async Task<IReadOnlyList<HotspotResponse>> GetHotspotsAsync(int days, CancellationToken cancellationToken = default)
    {
        days = Math.Clamp(days, 7, 365);
        var since = DateTimeOffset.UtcNow.AddDays(-days);
        var rows = await _db.Complaints.AsNoTracking().Where(entity => entity.CreatedAt >= since && entity.Status != ComplaintStatus.Withdrawn && entity.Status != ComplaintStatus.ClosedFraud)
            .Select(entity => new { entity.Latitude, entity.Longitude, entity.Category, entity.Priority }).ToListAsync(cancellationToken);
        return rows.GroupBy(entity => new { Lat = Math.Round(entity.Latitude, 3), Lon = Math.Round(entity.Longitude, 3) })
            .Select(group =>
            {
                var count = group.Count();
                var severity = group.Sum(item => (int)item.Priority);
                var risk = Math.Min(1m, (count / 10m) + (severity / Math.Max(1m, count * 12m)));
                var category = group.GroupBy(item => item.Category).OrderByDescending(item => item.Count()).First().Key;
                var level = risk >= 0.75m ? "Critical" : risk >= 0.50m ? "High" : risk >= 0.25m ? "Medium" : "Low";
                return new HotspotResponse(group.Key.Lat, group.Key.Lon, count, Math.Round(risk, 3), category, level);
            }).OrderByDescending(item => item.RiskScore).Take(100).ToList();
    }

    public async Task<AiMetricsResponse> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        var total = await _db.Complaints.AsNoTracking().CountAsync(entity => entity.AiTriagedAt != null, cancellationToken);
        var pending = await _db.AiTriageAnalyses.AsNoTracking().CountAsync(entity => entity.RequiresManualReview && entity.ReviewedAt == null, cancellationToken);
        var fraud = await _db.Complaints.AsNoTracking().CountAsync(entity => entity.Status == ComplaintStatus.FraudReview, cancellationToken);
        var duplicates = await _db.Complaints.AsNoTracking().CountAsync(entity => entity.Status == ComplaintStatus.Merged && entity.DuplicateOfComplaintId != null, cancellationToken);
        var confidence = await _db.AiTriageAnalyses.AsNoTracking().AverageAsync(entity => (decimal?)entity.ClassificationConfidence, cancellationToken) ?? 0m;
        return new AiMetricsResponse(total, pending, fraud, duplicates, Math.Round(confidence, 4), _options.UseGemini ? "Gemini" : "RuleBased", _options.UseGemini ? _options.Model : "civichero-rules-v1");
    }

    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.EnableBackgroundTriage) return 0;
        var ids = await _db.Complaints.AsNoTracking().Where(entity => entity.Status == ComplaintStatus.Created && entity.AiTriagedAt == null)
            .OrderBy(entity => entity.CreatedAt).Take(Math.Clamp(_options.BatchSize, 1, 100)).Select(entity => entity.Id).ToListAsync(cancellationToken);
        var processed = 0;
        foreach (var id in ids)
        {
            try { await AnalyzeComplaintAsync(id, false, cancellationToken); processed++; }
            catch when (!cancellationToken.IsCancellationRequested) { _db.ChangeTracker.Clear(); }
        }
        return processed;
    }

    private bool IsSupervisor() => string.Equals(_currentUser.Role, nameof(UserRole.Supervisor), StringComparison.OrdinalIgnoreCase);

    private bool IsAdministrator() => _currentUser.Role is nameof(UserRole.Admin) or nameof(UserRole.SuperAdmin);

    private void EnsureReviewRole()
    {
        if (!IsSupervisor() && !IsAdministrator())
            throw new UnauthorizedAccessException("Supervisor or administrator permission is required.");
    }

    private IQueryable<AiTriageAnalysis> ApplyReviewScope(IQueryable<AiTriageAnalysis> source)
    {
        if (!IsSupervisor()) return source;
        var departmentId = _currentUser.DepartmentId
            ?? throw new UnauthorizedAccessException("Supervisor department scope is missing.");
        source = source.Where(entity => entity.Complaint.DepartmentId == departmentId);
        if (_currentUser.WardId.HasValue)
            source = source.Where(entity => entity.Complaint.WardId == _currentUser.WardId.Value);
        return source;
    }

    private void EnsureComplaintReviewScope(Complaint complaint)
    {
        if (IsAdministrator()) return;
        if (!IsSupervisor())
            throw new UnauthorizedAccessException("Supervisor or administrator permission is required.");
        var departmentId = _currentUser.DepartmentId
            ?? throw new UnauthorizedAccessException("Supervisor department scope is missing.");
        if (complaint.DepartmentId != departmentId)
            throw new UnauthorizedAccessException("This complaint is outside the Supervisor's department scope.");
        if (_currentUser.WardId.HasValue && complaint.WardId != _currentUser.WardId.Value)
            throw new UnauthorizedAccessException("This complaint is outside the Supervisor's ward scope.");
    }

    private void EnsureDecisionAllowed(string decision)
    {
        if (string.IsNullOrWhiteSpace(decision))
            throw new ValidationException(["An AI review decision is required."]);

        var supervisorAllowed = new[] { "Clear", "OverrideRouting", "RejectInvalid", "Escalate", "Merge", "Reanalyze" };
        var administratorAllowed = new[] { "Clear", "OverrideRouting", "RejectInvalid", "ConfirmFraud", "Merge", "Reanalyze" };
        var allowed = IsSupervisor() ? supervisorAllowed : administratorAllowed;
        if (!allowed.Any(value => value.Equals(decision, StringComparison.OrdinalIgnoreCase)))
            throw new ValidationException([$"Decision '{decision}' is not permitted for the current role."]);
    }

    private static void RequireDetailedNotes(string? notes, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(notes) || notes.Trim().Length < 5)
            throw new ValidationException([$"{fieldName} must contain at least 5 characters."]);
    }

    private void MarkSupersededAnalyses(IEnumerable<AiTriageAnalysis> analyses)
    {
        var reviewedAt = DateTimeOffset.UtcNow;
        foreach (var analysis in analyses)
        {
            analysis.ReviewDecision = "Superseded";
            analysis.ReviewNotes = "A newer AI analysis was reviewed for this complaint.";
            analysis.ReviewedByUserId = RequireCurrentUserId();
            analysis.ReviewedAt = reviewedAt;
            analysis.RequiresManualReview = false;
        }
    }

    private long RequireCurrentUserId() => _currentUser.UserId
        ?? throw new UnauthorizedAccessException("Authenticated user identity is required.");

    private async Task<string> ApplyRoutingOverrideAsync(
        Complaint complaint,
        AiReviewDecisionRequest request,
        string reason,
        CancellationToken cancellationToken)
    {
        var category = request.Category?.Trim();
        if (string.IsNullOrWhiteSpace(category))
            throw new ValidationException(["Category is required for a routing override."]);
        if (!request.DepartmentId.HasValue || !request.WardId.HasValue)
            throw new ValidationException(["Department and ward are required for a routing override."]);

        var department = await _db.Departments.AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == request.DepartmentId.Value && entity.IsActive, cancellationToken)
            ?? throw new NotFoundException("The selected active department was not found.");
        var ward = await _db.Wards.AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == request.WardId.Value &&
                                           entity.DepartmentId == request.DepartmentId.Value &&
                                           entity.IsActive, cancellationToken)
            ?? throw new BusinessRuleViolationException("The selected ward is not active in the selected department.");

        if (IsSupervisor())
        {
            var departmentId = _currentUser.DepartmentId
                ?? throw new UnauthorizedAccessException("Supervisor department scope is missing.");
            if (department.Id != departmentId)
                throw new UnauthorizedAccessException("A Supervisor cannot route a complaint outside their department scope. Escalate it to Admin instead.");
            if (_currentUser.WardId.HasValue && ward.Id != _currentUser.WardId.Value)
                throw new UnauthorizedAccessException("A Supervisor cannot route a complaint outside their ward scope.");
        }

        var currentAssignment = await _db.ComplaintAssignments
            .Include(entity => entity.Officer)
            .FirstOrDefaultAsync(entity => entity.ComplaintId == complaint.Id && entity.IsCurrent, cancellationToken);
        var assignmentInvalidated = currentAssignment is not null &&
            (currentAssignment.Officer.DepartmentId != department.Id ||
             (currentAssignment.Officer.WardId.HasValue && currentAssignment.Officer.WardId.Value != ward.Id));

        if (assignmentInvalidated)
        {
            currentAssignment!.IsCurrent = false;
            currentAssignment.Status = AssignmentStatus.Cancelled;
            currentAssignment.ReassignedAt = DateTimeOffset.UtcNow;
            currentAssignment.Reason = $"AI review routing override: {reason}";
            complaint.AssignedOfficerId = null;
            complaint.Status = ComplaintStatus.ReassignmentPending;
        }
        else if (complaint.Status is ComplaintStatus.AiTriage or ComplaintStatus.FraudReview or ComplaintStatus.Created)
        {
            complaint.Status = ComplaintStatus.Created;
        }

        complaint.Category = category;
        complaint.DepartmentId = department.Id;
        complaint.WardId = ward.Id;
        complaint.DuplicateOfComplaintId = null;
        complaint.ClosedAt = null;

        return $"Routing changed to {department.Name} / {ward.Name}, category {category}." +
               (assignmentInvalidated ? " The incompatible current assignment was cancelled." : string.Empty);
    }

    private void AddReviewAudit(
        string action,
        Complaint complaint,
        AiTriageAnalysis analysis,
        object newValues,
        object? oldValues = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUser.UserId,
            UserEmail = _currentUser.Email,
            UserRole = _currentUser.Role,
            Action = action,
            EntityName = nameof(Complaint),
            EntityId = complaint.Id.ToString(),
            OldValuesJson = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
            NewValuesJson = JsonSerializer.Serialize(new
            {
                reviewAnalysisId = analysis.Id,
                complaintId = complaint.Id,
                values = newValues
            }),
            Severity = action.Contains("ESCALATED", StringComparison.OrdinalIgnoreCase) ? "Warning" : "Information",
            Success = true,
            HttpStatusCode = 200,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private async Task NotifyAdministratorsAsync(Complaint complaint, string reason, CancellationToken cancellationToken)
    {
        var adminIds = await _db.Users.AsNoTracking()
            .Where(entity => !entity.IsDeleted && entity.IsActive &&
                             (entity.Role == UserRole.Admin || entity.Role == UserRole.SuperAdmin))
            .Select(entity => entity.Id)
            .ToListAsync(cancellationToken);
        foreach (var adminId in adminIds)
        {
            await _notifications.SendAsync(new NotificationDispatchRequest(
                adminId,
                "AI review escalated by Supervisor",
                $"Complaint CH-{complaint.Id:000000} requires an administrator decision. Reason: {reason}",
                nameof(NotificationType.General),
                "Complaint",
                complaint.Id,
                "/admin/ai-review"), cancellationToken);
        }
    }

    private async Task NotifyCitizenAboutDecisionAsync(
        Complaint complaint,
        string decision,
        string? notes,
        CancellationToken cancellationToken)
    {
        var (title, message) = decision.ToLowerInvariant() switch
        {
            "clear" => ("Complaint review completed", "Your complaint passed human AI review and returned to the assignment queue."),
            "overriderouting" => ("Complaint routing reviewed", "Your complaint category and routing were corrected after human review."),
            "rejectinvalid" or "confirmfraud" => ("Complaint rejected after review", $"Your complaint was rejected after human review. Reason: {notes}"),
            "merge" => ("Complaint merged", $"Your complaint was linked to an existing complaint after duplicate review. Reason: {notes}"),
            _ => (string.Empty, string.Empty)
        };
        if (string.IsNullOrWhiteSpace(title)) return;

        await _notifications.SendAsync(new NotificationDispatchRequest(
            complaint.CitizenId,
            title,
            message,
            nameof(NotificationType.ComplaintProgress),
            "Complaint",
            complaint.Id,
            $"/citizen/complaints/{complaint.Id}"), cancellationToken);
    }

    private async Task<FraudCheckResponse> BuildFraudResponseAsync(AiTextRequest request, AiProviderResult result, CancellationToken cancellationToken)
    {
        var signals = new List<string>();
        var score = result.FraudScore;
        if (request.CitizenId.HasValue)
        {
            var since = DateTimeOffset.UtcNow.AddHours(-Math.Clamp(_options.RecentSubmissionWindowHours, 1, 168));
            var recent = await _db.Complaints.AsNoTracking().CountAsync(entity => entity.CitizenId == request.CitizenId && entity.CreatedAt >= since, cancellationToken);
            if (recent >= _options.ExcessiveSubmissionCount)
            {
                score = Math.Min(1m, score + 0.30m);
                signals.Add($"Citizen submitted {recent} complaints in the configured review window.");
            }
        }
        if (result.FraudScore >= 0.45m) signals.Add(result.Reasoning);
        if (signals.Count == 0) signals.Add("No strong spam or abuse signal was detected.");
        var verdict = score >= 0.75m ? "HighRisk" : score >= _options.FraudReviewThreshold ? "RiskFlag" : "Safe";
        return new FraudCheckResponse(verdict, Math.Round(score, 4), score >= _options.FraudReviewThreshold, signals, result.Provider);
    }

    private async Task<AiProviderResult> ProviderAnalysisAsync(AiTextRequest request, CancellationToken cancellationToken)
        => await _gemini.AnalyzeAsync(request.Title, request.Description, request.Category, cancellationToken)
            ?? await _rules.AnalyzeAsync(request.Title, request.Description, request.Category, cancellationToken)
            ?? throw new InvalidOperationException("No AI provider is available.");

    private async Task<Department?> FindDepartmentAsync(string category, CancellationToken cancellationToken)
    {
        var keywords = category switch
        {
            "Pothole" or "Road Damage" => new[] { "road", "public works", "PWD" },
            "Garbage" or "Illegal Dumping" => new[] { "sanitation", "solid waste", "garbage" },
            "Streetlight" => new[] { "electric", "streetlight", "lighting" },
            "Water Leakage" => new[] { "water", "hydraulic" },
            "Drainage" => new[] { "drainage", "sewer" },
            "Public Safety" => new[] { "safety", "emergency" },
            _ => Array.Empty<string>()
        };
        var departments = await _db.Departments.AsNoTracking().Where(entity => entity.IsActive).ToListAsync(cancellationToken);
        return departments.FirstOrDefault(entity => keywords.Any(keyword => entity.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) || entity.Code.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
    }

    private static AiTriageResponse Map(Complaint complaint, AiTriageAnalysis analysis) => new(
        complaint.Id, complaint.Status.ToString(),
        new ClassificationResponse(analysis.PredictedCategory, analysis.PredictedDepartmentId, null, analysis.ClassificationConfidence, analysis.Provider, analysis.Reasoning),
        new DuplicateCheckResponse(analysis.DuplicateStatus, analysis.DuplicateScore, analysis.DuplicateComplaintId, [], analysis.Reasoning),
        new FraudCheckResponse(analysis.FraudVerdict, analysis.FraudScore, analysis.RequiresManualReview, [analysis.Reasoning], analysis.Provider),
        new PriorityPredictionResponse(analysis.PredictedPriority.ToString(), analysis.PriorityScore, [analysis.Reasoning], analysis.Provider),
        analysis.RequiresManualReview, analysis.AnalyzedAt);

    private static decimal TextSimilarity(string left, string right)
    {
        var a = RuleBasedAiService.Tokenize(left).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var b = RuleBasedAiService.Tokenize(right).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (a.Count == 0 || b.Count == 0) return 0m;
        var intersection = a.Intersect(b, StringComparer.OrdinalIgnoreCase).Count();
        return (2m * intersection) / (a.Count + b.Count);
    }

    private static double HaversineMeters(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
    {
        const double earthRadius = 6371000d;
        var dLat = ToRadians((double)(lat2 - lat1));
        var dLon = ToRadians((double)(lon2 - lon1));
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(ToRadians((double)lat1)) * Math.Cos(ToRadians((double)lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return earthRadius * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
    private static double ToRadians(double degrees) => degrees * Math.PI / 180d;
}
