using CivicHero.Backend.Core.DTOs.Common;
using CivicHero.Backend.Core.DTOs.VisualVerification;
using CivicHero.Backend.Core.Entities;

namespace CivicHero.Backend.Core.Services;

public interface IVisualVerificationProvider
{
    Task<VisualProviderResult?> AnalyzeAsync(Complaint complaint, IReadOnlyList<ComplaintImage> beforeImages, IReadOnlyList<ComplaintImage> afterImages, CancellationToken cancellationToken = default);
}

public interface IVisualVerificationService
{
    Task<VisualVerificationResponse> AnalyzeComplaintAsync(long complaintId, CancellationToken cancellationToken = default);
    Task<PagedResponse<VisualVerificationResponse>> GetQueueAsync(VisualVerificationQuery query, CancellationToken cancellationToken = default);
    Task<VisualVerificationResponse> GetByIdAsync(long analysisId, CancellationToken cancellationToken = default);
    Task<VisualVerificationResponse> ReviewAsync(long analysisId, VisualVerificationHumanDecisionRequest request, CancellationToken cancellationToken = default);
}
