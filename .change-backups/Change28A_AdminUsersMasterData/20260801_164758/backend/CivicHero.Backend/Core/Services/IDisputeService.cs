using CivicHero.Backend.Core.DTOs.Disputes;
using CivicHero.Backend.Infrastructure.Storage;
using Microsoft.AspNetCore.Http;

namespace CivicHero.Backend.Core.Services;

public interface IDisputeService
{
    Task<IReadOnlyList<DisputeResponse>> MineAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DisputeResponse>> OfficerMineAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DisputeResponse>> QueueAsync(bool appealsOnly, CancellationToken cancellationToken = default);
    Task<DisputeResponse> GetAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DisputeHistoryResponse>> HistoryAsync(long id, CancellationToken cancellationToken = default);
    Task<DisputeResponse> RaiseAsync(long complaintId, RaiseDisputeRequest request, CancellationToken cancellationToken = default);
    Task<DisputeResponse> UploadEvidenceAsync(long id, IReadOnlyList<IFormFile> evidence, long? requestId, CancellationToken cancellationToken = default);
    Task<StorageDownload> DownloadEvidenceAsync(long id, long evidenceId, CancellationToken cancellationToken = default);
    Task<DisputeEvidenceRequestResponse> RequestEvidenceAsync(long id, DisputeEvidenceRequest request, CancellationToken cancellationToken = default);
    Task<DisputeResponse> SupervisorDecisionAsync(long id, DisputeDecisionRequest request, CancellationToken cancellationToken = default);
    Task<DisputeResponse> ReopenRequestAsync(long id, ReopenDisputeRequest request, CancellationToken cancellationToken = default);
    Task<DisputeResponse> AppealAsync(long id, AppealDisputeRequest request, CancellationToken cancellationToken = default);
    Task<DisputeResponse> AdminDecisionAsync(long id, DisputeDecisionRequest request, CancellationToken cancellationToken = default);
    Task<DisputeResponse> SuperAdminDecisionAsync(long id, DisputeDecisionRequest request, CancellationToken cancellationToken = default);
}
