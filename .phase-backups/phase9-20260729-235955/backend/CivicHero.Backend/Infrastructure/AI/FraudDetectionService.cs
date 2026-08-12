using CivicHero.Backend.Core.DTOs.Ai;
using CivicHero.Backend.Core.Services;

namespace CivicHero.Backend.Infrastructure.AI;

public sealed class FraudDetectionService
{
    private readonly IAiTriageService _triage;
    public FraudDetectionService(IAiTriageService triage) => _triage = triage;
    public Task<FraudCheckResponse> CheckAsync(AiTextRequest request, CancellationToken cancellationToken = default) => _triage.CheckFraudAsync(request, cancellationToken);
}
