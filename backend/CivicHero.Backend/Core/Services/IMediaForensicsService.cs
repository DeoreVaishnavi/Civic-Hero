using CivicHero.Backend.Core.DTOs.Ai;

namespace CivicHero.Backend.Core.Services;

public interface IMediaForensicsService
{
    Task<MediaForensicsReportResponse> AnalyzeComplaintMediaAsync(
        long complaintId,
        CancellationToken cancellationToken = default);
}
