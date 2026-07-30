using CivicHero.Backend.Core.DTOs.Ai;
using CivicHero.Backend.Core.Services;

namespace CivicHero.Backend.Infrastructure.AI;

public sealed class ClassificationService
{
    private readonly IAiTriageService _triage;
    public ClassificationService(IAiTriageService triage) => _triage = triage;
    public Task<ClassificationResponse> ClassifyAsync(AiTextRequest request, CancellationToken cancellationToken = default) => _triage.ClassifyAsync(request, cancellationToken);
}
