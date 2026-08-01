using Microsoft.AspNetCore.Http;

namespace CivicHero.Backend.Core.DTOs.Verification;

public sealed class VerifyComplaintRequest
{
    public string? Decision { get; set; }
    public bool? Approved { get; set; }
    public int? Rating { get; set; }
    public string? Remarks { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
}

public sealed record GeoVerifyRequest(decimal Latitude, decimal Longitude);

public sealed class VerificationEvidenceUploadRequest
{
    public List<IFormFile> Evidence { get; set; } = new();
}

public sealed record SupervisorVerificationDecisionRequest(bool ApproveCitizen, string Remarks);

public sealed record VerificationEvidenceItem(
    long Id,
    string FileName,
    long FileSize,
    string MimeType,
    DateTimeOffset UploadedAt,
    string DownloadPath);

public sealed record VerificationResponse(
    long ComplaintId,
    string ReferenceNumber,
    string Title,
    string Status,
    string Decision,
    int? Rating,
    string? Remarks,
    double? DistanceMetres,
    DateTimeOffset DueAt,
    DateTimeOffset? CompletedAt,
    bool CanVerify,
    bool CanAmend,
    bool CanWithdraw,
    bool CanRemind,
    IReadOnlyList<VerificationEvidenceItem> CitizenEvidence);

public sealed record VerificationQueueItem(
    long ComplaintId,
    string ReferenceNumber,
    string Title,
    string Priority,
    string DepartmentName,
    string WardName,
    DateTimeOffset DueAt,
    long RemainingMinutes,
    bool IsOverdue,
    string Decision,
    bool RequiresSupervisorDecision);


public sealed record VerificationDecisionCycleItem(
    int CycleNumber,
    string CitizenDecision,
    string CitizenRemarks,
    string Status,
    DateTimeOffset RaisedAt,
    string? SupervisorDecision,
    string? SupervisorRemarks,
    DateTimeOffset? ReviewedAt,
    DateTimeOffset? ResolvedAt);

public sealed record VerificationHistoryItem(
    long VerificationId,
    long ComplaintId,
    string ReferenceNumber,
    string Title,
    string DepartmentName,
    string WardName,
    string ComplaintStatus,
    string Decision,
    int? Rating,
    string? Remarks,
    DateTimeOffset DueAt,
    DateTimeOffset? CompletedAt,
    int CitizenEvidenceCount,
    string? SupervisorDecision,
    string? SupervisorRemarks,
    DateTimeOffset? SupervisorReviewedAt,
    IReadOnlyList<VerificationDecisionCycleItem> DecisionCycles);
