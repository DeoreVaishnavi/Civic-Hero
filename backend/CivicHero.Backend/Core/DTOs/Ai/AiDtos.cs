namespace CivicHero.Backend.Core.DTOs.Ai;

public sealed record AiTextRequest(string Title, string Description, string? Category = null, decimal? Latitude = null, decimal? Longitude = null, long? CitizenId = null);
public sealed record ClassificationResponse(string Category, long? DepartmentId, string? DepartmentName, decimal Confidence, string Provider, string Reasoning);
public sealed record DuplicateMatchResponse(long ComplaintId, string Title, decimal DistanceMeters, decimal TextSimilarity, decimal CombinedScore);
public sealed record DuplicateCheckResponse(string Status, decimal Score, long? MatchingComplaintId, IReadOnlyList<DuplicateMatchResponse> Matches, string Reasoning);
public sealed record FraudCheckResponse(string Verdict, decimal Score, bool RequiresManualReview, IReadOnlyList<string> Signals, string Provider);
public sealed record PriorityPredictionResponse(string Priority, decimal Score, IReadOnlyList<string> Signals, string Provider);
public sealed record AiTriageResponse(long ComplaintId, string Status, ClassificationResponse Classification, DuplicateCheckResponse Duplicate, FraudCheckResponse Fraud, PriorityPredictionResponse Priority, bool RequiresManualReview, DateTimeOffset AnalyzedAt);

public sealed record AiReviewQueueItem(
    long ComplaintId,
    string Title,
    string CitizenName,
    string Status,
    long DepartmentId,
    string DepartmentName,
    long WardId,
    string WardName,
    string CurrentCategory,
    string PredictedCategory,
    long? PredictedDepartmentId,
    string? PredictedDepartmentName,
    string PredictedPriority,
    string DuplicateStatus,
    long? DuplicateComplaintId,
    decimal DuplicateScore,
    string FraudVerdict,
    decimal FraudScore,
    string Reasoning,
    bool EscalatedToAdmin,
    string? EscalationNotes,
    DateTimeOffset AnalyzedAt);

public sealed record AiReviewDecisionRequest(
    string Decision,
    string? Notes,
    long? MergeIntoComplaintId = null,
    string? Category = null,
    long? DepartmentId = null,
    long? WardId = null);

public sealed record HotspotResponse(decimal Latitude, decimal Longitude, int ComplaintCount, decimal RiskScore, string DominantCategory, string RiskLevel);
public sealed record AiMetricsResponse(int TriagedComplaints, int ManualReviewPending, int FraudReviewCount, int ConfirmedDuplicates, decimal AverageConfidence, string ActiveProvider, string Model);
public sealed record RetrainRequest(string? Notes);
