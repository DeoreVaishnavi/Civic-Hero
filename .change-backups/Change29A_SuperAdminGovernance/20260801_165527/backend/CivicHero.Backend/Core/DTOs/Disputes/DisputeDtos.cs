namespace CivicHero.Backend.Core.DTOs.Disputes;

public sealed record RaiseDisputeRequest(string Reason);
public sealed record DisputeDecisionRequest(string Decision, string Remarks);
public sealed record AppealDisputeRequest(string Remarks);
public sealed record ReopenDisputeRequest(string Reason);
public sealed record DisputeEvidenceRequest(string TargetRole, string Message, DateTimeOffset? DueAt);

public sealed record DisputeEvidenceResponse(
    long Id,
    long DisputeId,
    string FileName,
    long FileSize,
    string MimeType,
    string SourceRole,
    long UploadedByUserId,
    DateTimeOffset UploadedAt,
    long? RequestId,
    string DownloadPath);

public sealed record DisputeEvidenceRequestResponse(
    long Id,
    long DisputeId,
    string TargetRole,
    string Message,
    DateTimeOffset RequestedAt,
    DateTimeOffset? DueAt,
    long RequestedByUserId,
    bool IsFulfilled,
    DateTimeOffset? FulfilledAt);

public sealed record DisputeResponse(
    long Id,
    long ComplaintId,
    string ReferenceNumber,
    string Title,
    string Status,
    int CycleNumber,
    string CitizenRemarks,
    string? SupervisorDecision,
    string? SupervisorRemarks,
    string? AdminDecision,
    string? AdminRemarks,
    DateTimeOffset RaisedAt,
    DateTimeOffset? AppealDeadline,
    bool CanAppeal,
    bool RequiresSuperAdmin = false,
    DateTimeOffset? ReopenDeadline = null,
    bool CanRequestReopen = false,
    bool CanUploadCitizenEvidence = false,
    bool CanUploadOfficerEvidence = false,
    IReadOnlyList<DisputeEvidenceResponse>? Evidence = null,
    IReadOnlyList<DisputeEvidenceRequestResponse>? EvidenceRequests = null);

public sealed record DisputeHistoryResponse(
    long Id,
    long ComplaintId,
    int CycleNumber,
    string Status,
    string CitizenRemarks,
    string? SupervisorDecision,
    string? SupervisorRemarks,
    string? AdminDecision,
    string? AdminRemarks,
    DateTimeOffset RaisedAt,
    DateTimeOffset? ReviewedAt,
    DateTimeOffset? ResolvedAt,
    DateTimeOffset? AppealDeadline);
