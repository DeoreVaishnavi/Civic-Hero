using CivicHero.Backend.Core.DTOs.Ai;
using CivicHero.Backend.Core.Services;

namespace CivicHero.Backend.Infrastructure.AI;

public sealed class DuplicateDetectionService
{
    private readonly IAiTriageService _triage;
    public DuplicateDetectionService(IAiTriageService triage) => _triage = triage;
    public Task<DuplicateCheckResponse> CheckAsync(AiTextRequest request, long? excludeComplaintId = null, CancellationToken cancellationToken = default) => _triage.CheckDuplicateAsync(request, excludeComplaintId, cancellationToken);
}
