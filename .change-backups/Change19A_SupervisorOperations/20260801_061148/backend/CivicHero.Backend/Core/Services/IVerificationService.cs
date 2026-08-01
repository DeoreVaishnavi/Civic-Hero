using CivicHero.Backend.Core.DTOs.Verification;
namespace CivicHero.Backend.Core.Services;
public interface IVerificationService
{
 Task<IReadOnlyList<VerificationQueueItem>> GetPendingAsync(CancellationToken cancellationToken=default);
 Task<IReadOnlyList<VerificationQueueItem>> GetSupervisorQueueAsync(bool overdueOnly, CancellationToken cancellationToken=default);
 Task<VerificationResponse> GetAsync(long complaintId, CancellationToken cancellationToken=default);
 Task<object> CheckGeoAsync(long complaintId, GeoVerifyRequest request, CancellationToken cancellationToken=default);
 Task<VerificationResponse> VerifyAsync(long complaintId, VerifyComplaintRequest request, CancellationToken cancellationToken=default);
 Task<VerificationResponse> RemindAsync(long complaintId, CancellationToken cancellationToken=default);
}
