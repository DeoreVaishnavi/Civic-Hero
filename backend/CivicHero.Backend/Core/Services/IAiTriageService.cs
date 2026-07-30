using CivicHero.Backend.Core.DTOs.Ai;

namespace CivicHero.Backend.Core.Services;

public interface IAiTriageService
{
    Task<ClassificationResponse> ClassifyAsync(AiTextRequest request, CancellationToken cancellationToken = default);
    Task<DuplicateCheckResponse> CheckDuplicateAsync(AiTextRequest request, long? excludeComplaintId = null, CancellationToken cancellationToken = default);
    Task<FraudCheckResponse> CheckFraudAsync(AiTextRequest request, CancellationToken cancellationToken = default);
    Task<PriorityPredictionResponse> PredictPriorityAsync(AiTextRequest request, CancellationToken cancellationToken = default);
    Task<AiTriageResponse> AnalyzeComplaintAsync(long complaintId, bool force = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AiReviewQueueItem>> GetReviewQueueAsync(CancellationToken cancellationToken = default);
    Task<AiTriageResponse> DecideAsync(long complaintId, AiReviewDecisionRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HotspotResponse>> GetHotspotsAsync(int days, CancellationToken cancellationToken = default);
    Task<AiMetricsResponse> GetMetricsAsync(CancellationToken cancellationToken = default);
    Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default);
}
