namespace CivicHero.Backend.Infrastructure.AI;

public sealed class OpenAiService : IAiService
{
    private readonly ILogger<OpenAiService> _logger;
    public OpenAiService(ILogger<OpenAiService> logger) => _logger = logger;
    public Task<AiProviderResult?> AnalyzeAsync(string title, string description, string? currentCategory, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("OpenAI provider is reserved for a future configuration; CivicHero is using its active provider and rule fallback.");
        return Task.FromResult<AiProviderResult?>(null);
    }
}
