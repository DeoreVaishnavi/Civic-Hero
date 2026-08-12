using CivicHero.Backend.Core.DTOs.Ai;
using CivicHero.Backend.Core.Services;

namespace CivicHero.Backend.Infrastructure.AI;

public sealed class PriorityPredictionService
{
    private readonly IAiTriageService _triage;
    public PriorityPredictionService(IAiTriageService triage) => _triage = triage;
    public Task<PriorityPredictionResponse> PredictAsync(AiTextRequest request, CancellationToken cancellationToken = default) => _triage.PredictPriorityAsync(request, cancellationToken);
}
