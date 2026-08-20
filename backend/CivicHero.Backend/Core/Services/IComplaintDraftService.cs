using CivicHero.Backend.Core.DTOs.Complaints;
using CivicHero.Backend.Infrastructure.Storage;

namespace CivicHero.Backend.Core.Services;

public interface IComplaintDraftService
{
    Task<ComplaintDraftResponse?> GetCurrentAsync(CancellationToken cancellationToken = default);
    Task<ComplaintDraftResponse> SaveCurrentAsync(UpsertComplaintDraftRequest request, CancellationToken cancellationToken = default);
    Task<ComplaintDraftEvidenceResponse> AddEvidenceAsync(Microsoft.AspNetCore.Http.IFormFile evidence, CancellationToken cancellationToken = default);
    Task RemoveEvidenceAsync(long evidenceId, CancellationToken cancellationToken = default);
    Task<StorageDownload> DownloadEvidenceAsync(long evidenceId, CancellationToken cancellationToken = default);
    Task DeleteCurrentAsync(CancellationToken cancellationToken = default);
}
