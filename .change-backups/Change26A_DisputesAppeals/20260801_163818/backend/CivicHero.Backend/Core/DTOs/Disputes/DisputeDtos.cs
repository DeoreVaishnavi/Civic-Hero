namespace CivicHero.Backend.Core.DTOs.Disputes;
public sealed record RaiseDisputeRequest(string Reason);
public sealed record DisputeDecisionRequest(string Decision, string Remarks);
public sealed record AppealDisputeRequest(string Remarks);
public sealed record DisputeResponse(long Id, long ComplaintId, string ReferenceNumber, string Title, string Status, int CycleNumber, string CitizenRemarks, string? SupervisorDecision, string? SupervisorRemarks, string? AdminDecision, string? AdminRemarks, DateTimeOffset RaisedAt, DateTimeOffset? AppealDeadline, bool CanAppeal);

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
