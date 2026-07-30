using CivicHero.Backend.Core.DTOs.Common;
using CivicHero.Backend.Core.DTOs.Emergency;
using CivicHero.Backend.Core.Entities;

namespace CivicHero.Backend.Core.Services;

public interface IEmergencyReviewService
{
    ComplaintEmergencyReview CreatePendingReview(Complaint complaint, string? reason);
    Task<PagedResponse<EmergencyReviewResponse>> GetAsync(EmergencyReviewQuery query, CancellationToken cancellationToken = default);
    Task<EmergencyReviewResponse> DecideAsync(long reviewId, EmergencyReviewDecisionRequest request, CancellationToken cancellationToken = default);
}
