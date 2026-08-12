namespace CivicHero.Backend.Infrastructure.AI;

public interface IAiProvider
{
    Task<AiProviderResult?> AnalyzeAsync(string title, string description, string? currentCategory, CancellationToken cancellationToken = default);
}
