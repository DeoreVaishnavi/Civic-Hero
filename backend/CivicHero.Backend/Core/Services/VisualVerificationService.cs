using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CivicHero.Backend.Core.DTOs.Common;
using CivicHero.Backend.Core.DTOs.VisualVerification;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.AI;
using CivicHero.Backend.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Core.Services;

public sealed class VisualVerificationService : IVisualVerificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly GeminiVisualVerificationProvider _gemini;
    private readonly RuleBasedVisualVerificationProvider _fallback;
    private readonly IOptionsMonitor<VisualVerificationOptions> _options;

    public VisualVerificationService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        GeminiVisualVerificationProvider gemini,
        RuleBasedVisualVerificationProvider fallback,
        IOptionsMonitor<VisualVerificationOptions> options)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _gemini = gemini;
        _fallback = fallback;
        _options = options;
    }

    public async Task<VisualVerificationResponse> AnalyzeComplaintAsync(long complaintId, CancellationToken cancellationToken = default)
    {
        var complaint = await LoadComplaintAsync(complaintId, tracking: true, cancellationToken);
        if (_currentUser.UserId.HasValue) EnsureScope(complaint);
        var before = complaint.Images.Where(item => !item.IsResolutionEvidence).OrderBy(item => item.UploadedAt).ToArray();
        var after = complaint.Images.Where(item => item.IsResolutionEvidence).OrderBy(item => item.UploadedAt).ToArray();
        var evidenceFingerprint = Fingerprint(before, after);
        var existing = await _unitOfWork.Repository<VisualVerificationAnalysis>().Query()
            .Include(entity => entity.ReviewedByUser)
            .FirstOrDefaultAsync(entity => entity.ComplaintId == complaint.Id && entity.EvidenceFingerprint == evidenceFingerprint, cancellationToken);
        if (existing is not null)
        {
            existing.Complaint = complaint;
            return Map(existing);
        }
        var result = await _gemini.AnalyzeAsync(complaint, before, after, cancellationToken)
                     ?? await _fallback.AnalyzeAsync(complaint, before, after, cancellationToken)
                     ?? throw new BusinessRuleViolationException("Visual verification could not be completed.");
        var verdict = Enum.TryParse<VisualVerificationVerdict>(result.Verdict, true, out var parsed)
            ? parsed : VisualVerificationVerdict.NeedsHumanReview;
        var threshold = Math.Clamp(_options.CurrentValue.HumanReviewThreshold, 0.4m, 0.95m);
        var requiresReview = verdict is not VisualVerificationVerdict.LooksResolved || result.OverallConfidence < threshold || result.ManipulationRiskScore >= 0.45m;
        var analysis = new VisualVerificationAnalysis
        {
            ComplaintId = complaint.Id,
            Provider = result.Provider,
            Model = result.Model,
            Verdict = verdict,
            CompletionScore = result.CompletionScore,
            ImageQualityScore = result.ImageQualityScore,
            ManipulationRiskScore = result.ManipulationRiskScore,
            OverallConfidence = result.OverallConfidence,
            Reasoning = result.Reasoning,
            ObservationsJson = JsonSerializer.Serialize(result.Observations),
            BeforeImageIdsJson = JsonSerializer.Serialize(before.Select(image => image.Id).ToArray()),
            AfterImageIdsJson = JsonSerializer.Serialize(after.Select(image => image.Id).ToArray()),
            EvidenceFingerprint = evidenceFingerprint,
            RawResponse = result.RawResponse,
            RequiresHumanReview = requiresReview
        };
        await _unitOfWork.Repository<VisualVerificationAnalysis>().AddAsync(analysis, cancellationToken);
        complaint.Timeline.Add(new ComplaintTimeline
        {
            UserId = _currentUser.UserId,
            EventType = "VISUAL_VERIFICATION_COMPLETED",
            Description = $"Advisory visual verification result: {verdict}. Human review required: {requiresReview}.",
            Timestamp = DateTimeOffset.UtcNow
        });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        analysis.Complaint = complaint;
        return Map(analysis);
    }

    public async Task<PagedResponse<VisualVerificationResponse>> GetQueueAsync(VisualVerificationQuery query, CancellationToken cancellationToken = default)
    {
        EnsureReviewer();
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var source = _unitOfWork.Repository<VisualVerificationAnalysis>().Query()
            .Include(entity => entity.Complaint).ThenInclude(entity => entity.Department)
            .Include(entity => entity.Complaint).ThenInclude(entity => entity.Ward)
            .Include(entity => entity.Complaint).ThenInclude(entity => entity.Images)
            .Include(entity => entity.ReviewedByUser)
            .AsQueryable();
        source = ApplyScope(source);
        if (query.ReviewRequiredOnly) source = source.Where(entity => entity.RequiresHumanReview && entity.ReviewedAt == null);
        var total = await source.CountAsync(cancellationToken);
        var items = await source.OrderByDescending(entity => entity.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResponse<VisualVerificationResponse>
        {
            Items = items.Select(Map).ToArray(), Page = page, PageSize = pageSize, TotalCount = total
        };
    }

    public async Task<VisualVerificationResponse> GetByIdAsync(long analysisId, CancellationToken cancellationToken = default)
    {
        EnsureReviewer();
        var analysis = await _unitOfWork.Repository<VisualVerificationAnalysis>().Query()
            .Include(entity => entity.Complaint).ThenInclude(entity => entity.Department)
            .Include(entity => entity.Complaint).ThenInclude(entity => entity.Ward)
            .Include(entity => entity.Complaint).ThenInclude(entity => entity.Images)
            .Include(entity => entity.ReviewedByUser)
            .FirstOrDefaultAsync(entity => entity.Id == analysisId, cancellationToken)
            ?? throw new NotFoundException("Visual verification analysis was not found.");
        EnsureScope(analysis.Complaint);
        return Map(analysis);
    }

    public async Task<VisualVerificationResponse> ReviewAsync(long analysisId, VisualVerificationHumanDecisionRequest request, CancellationToken cancellationToken = default)
    {
        EnsureReviewer();
        var analysis = await _unitOfWork.Repository<VisualVerificationAnalysis>().Query(true)
            .Include(entity => entity.Complaint).ThenInclude(entity => entity.Department)
            .Include(entity => entity.Complaint).ThenInclude(entity => entity.Ward)
            .Include(entity => entity.Complaint).ThenInclude(entity => entity.Images)
            .Include(entity => entity.ReviewedByUser)
            .FirstOrDefaultAsync(entity => entity.Id == analysisId, cancellationToken)
            ?? throw new NotFoundException("Visual verification analysis was not found.");
        EnsureScope(analysis.Complaint);
        if (analysis.ReviewedAt.HasValue) throw new BusinessRuleViolationException("This analysis has already been reviewed.");
        var decision = request.Decision.Trim();
        if (decision is not ("ConfirmEvidence" or "RequestRework" or "DismissFlag"))
            throw new ValidationException(["Decision must be ConfirmEvidence, RequestRework or DismissFlag."]);
        if (request.Notes.Trim().Length is < 5 or > 1500)
            throw new ValidationException(["Review notes must contain 5 to 1500 characters."]);

        analysis.HumanDecision = decision;
        analysis.HumanNotes = request.Notes.Trim();
        analysis.ReviewedByUserId = RequireUserId();
        analysis.ReviewedAt = DateTimeOffset.UtcNow;
        analysis.RequiresHumanReview = false;
        if (decision == "RequestRework")
        {
            analysis.Complaint.Status = ComplaintStatus.ReassignmentPending;
            analysis.Complaint.AssignedOfficerId = null;
            analysis.Complaint.Timeline.Add(Timeline("VISUAL_REWORK_REQUESTED", $"Supervisor requested rework after visual review: {analysis.HumanNotes}"));
        }
        else
        {
            analysis.Complaint.Timeline.Add(Timeline("VISUAL_REVIEW_RECORDED", $"Human visual review decision: {decision}. {analysis.HumanNotes}"));
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(analysis);
    }

    private async Task<Complaint> LoadComplaintAsync(long id, bool tracking, CancellationToken cancellationToken) =>
        await _unitOfWork.Repository<Complaint>().Query(tracking)
            .Include(entity => entity.Images)
            .Include(entity => entity.Department)
            .Include(entity => entity.Ward)
            .Include(entity => entity.Timeline)
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken)
        ?? throw new NotFoundException("Complaint was not found.");

    private IQueryable<VisualVerificationAnalysis> ApplyScope(IQueryable<VisualVerificationAnalysis> source)
    {
        if (IsAdmin()) return source;
        var departmentId = _currentUser.DepartmentId ?? -1;
        return source.Where(entity => entity.Complaint.DepartmentId == departmentId);
    }
    private void EnsureScope(Complaint complaint)
    {
        if (IsAdmin()) return;
        if (_currentUser.Role == nameof(UserRole.Supervisor) && complaint.DepartmentId == _currentUser.DepartmentId) return;
        throw new UnauthorizedAccessException("This analysis is outside your department scope.");
    }
    private void EnsureReviewer()
    {
        if (_currentUser.Role is not (nameof(UserRole.Supervisor) or nameof(UserRole.Admin) or nameof(UserRole.SuperAdmin)))
            throw new UnauthorizedAccessException("Supervisor or administrator permission is required.");
    }
    private bool IsAdmin() => _currentUser.Role is nameof(UserRole.Admin) or nameof(UserRole.SuperAdmin);
    private long RequireUserId() => _currentUser.UserId ?? throw new UnauthorizedAccessException("Authentication is required.");
    private ComplaintTimeline Timeline(string type, string description) => new() { UserId = RequireUserId(), EventType = type, Description = description, Timestamp = DateTimeOffset.UtcNow };
    private static string Reference(Complaint complaint) => $"CH-{complaint.CreatedAt:yyyy}-{complaint.Id:D6}";
    private static VisualVerificationResponse Map(VisualVerificationAnalysis entity)
    {
        IReadOnlyList<string> observations;
        long[] beforeIds;
        long[] afterIds;
        try { observations = JsonSerializer.Deserialize<string[]>(entity.ObservationsJson) ?? []; }
        catch { observations = []; }
        try { beforeIds = JsonSerializer.Deserialize<long[]>(entity.BeforeImageIdsJson) ?? []; }
        catch { beforeIds = []; }
        try { afterIds = JsonSerializer.Deserialize<long[]>(entity.AfterImageIdsJson) ?? []; }
        catch { afterIds = []; }
        var beforeSet = beforeIds.ToHashSet();
        var afterSet = afterIds.ToHashSet();
        var beforeImages = entity.Complaint.Images.Where(image => beforeSet.Count == 0 ? !image.IsResolutionEvidence : beforeSet.Contains(image.Id));
        var afterImages = entity.Complaint.Images.Where(image => afterSet.Count == 0 ? image.IsResolutionEvidence : afterSet.Contains(image.Id));
        return new VisualVerificationResponse
        {
            Id = entity.Id, ComplaintId = entity.ComplaintId, ReferenceNumber = Reference(entity.Complaint), ComplaintTitle = entity.Complaint.Title,
            DepartmentName = entity.Complaint.Department.Name, WardName = entity.Complaint.Ward.Name,
            Provider = entity.Provider, Model = entity.Model, Verdict = entity.Verdict.ToString(), CompletionScore = entity.CompletionScore,
            ImageQualityScore = entity.ImageQualityScore, ManipulationRiskScore = entity.ManipulationRiskScore, OverallConfidence = entity.OverallConfidence,
            Reasoning = entity.Reasoning, Observations = observations, EvidenceFingerprint = entity.EvidenceFingerprint,
            BeforeImages = beforeImages.OrderBy(image => image.UploadedAt).Select(image => new VisualEvidenceImageResponse { Id = image.Id, FileName = image.FileName, MimeType = image.MimeType, DownloadPath = $"/complaints/{entity.ComplaintId}/images/{image.Id}" }).ToArray(),
            AfterImages = afterImages.OrderBy(image => image.UploadedAt).Select(image => new VisualEvidenceImageResponse { Id = image.Id, FileName = image.FileName, MimeType = image.MimeType, DownloadPath = $"/complaints/{entity.ComplaintId}/images/{image.Id}" }).ToArray(),
            RequiresHumanReview = entity.RequiresHumanReview,
            HumanDecision = entity.HumanDecision, HumanNotes = entity.HumanNotes, ReviewedByName = entity.ReviewedByUser?.FullName,
            CreatedAt = entity.CreatedAt, ReviewedAt = entity.ReviewedAt
        };
    }
    private static string Fingerprint(IEnumerable<ComplaintImage> before, IEnumerable<ComplaintImage> after)
    {
        var material = string.Join("|", before.Concat(after).OrderBy(image => image.Id).Select(image =>
            $"{image.Id}:{image.S3Key}:{image.FileSize}:{image.MimeType}:{image.UploadedAt:O}:{image.IsResolutionEvidence}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
    }

}
