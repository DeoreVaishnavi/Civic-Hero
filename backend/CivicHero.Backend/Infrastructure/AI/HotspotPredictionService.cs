using CivicHero.Backend.Core.DTOs.Ai;
using CivicHero.Backend.Core.Services;

namespace CivicHero.Backend.Infrastructure.AI;

public sealed class HotspotPredictionService
{
    private readonly IAiTriageService _triage;
    public HotspotPredictionService(IAiTriageService triage) => _triage = triage;
    public Task<IReadOnlyList<HotspotResponse>> PredictAsync(int days = 30, CancellationToken cancellationToken = default) => _triage.GetHotspotsAsync(days, cancellationToken);
}
