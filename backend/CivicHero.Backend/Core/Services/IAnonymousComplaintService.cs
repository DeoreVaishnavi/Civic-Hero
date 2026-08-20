using CivicHero.Backend.Core.DTOs.Anonymous;

namespace CivicHero.Backend.Core.Services;

public interface IAnonymousComplaintService
{
    Task<AnonymousComplaintCreatedResponse> CreateAsync(CreateAnonymousComplaintRequest request, string? remoteIp, CancellationToken cancellationToken = default);
    Task<AnonymousComplaintTrackingResponse> TrackAsync(string referenceNumber, string trackingToken, CancellationToken cancellationToken = default);
}
