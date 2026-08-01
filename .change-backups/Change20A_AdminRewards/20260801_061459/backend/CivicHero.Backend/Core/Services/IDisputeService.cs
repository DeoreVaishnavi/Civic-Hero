using CivicHero.Backend.Core.DTOs.Disputes;
namespace CivicHero.Backend.Core.Services;
public interface IDisputeService
{
 Task<IReadOnlyList<DisputeResponse>> MineAsync(CancellationToken cancellationToken=default);
 Task<IReadOnlyList<DisputeResponse>> QueueAsync(bool appealsOnly, CancellationToken cancellationToken=default);
 Task<DisputeResponse> GetAsync(long id, CancellationToken cancellationToken=default);
 Task<DisputeResponse> RaiseAsync(long complaintId, RaiseDisputeRequest request, CancellationToken cancellationToken=default);
 Task<DisputeResponse> SupervisorDecisionAsync(long id, DisputeDecisionRequest request, CancellationToken cancellationToken=default);
 Task<DisputeResponse> AppealAsync(long id, AppealDisputeRequest request, CancellationToken cancellationToken=default);
 Task<DisputeResponse> AdminDecisionAsync(long id, DisputeDecisionRequest request, CancellationToken cancellationToken=default);
}
