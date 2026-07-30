
namespace CivicHero.Backend.Core.Services;

public sealed record IntegrationDependencyStatus(string Name, string Status, string Detail, bool Required);
public sealed record IntegrationReadinessResponse(string OverallStatus, DateTimeOffset CheckedAtUtc, IReadOnlyList<IntegrationDependencyStatus> Dependencies, IReadOnlyList<string> AutomationWorkers, bool TwoFactorAvailable);

public interface IIntegrationReadinessService
{
    Task<IntegrationReadinessResponse> GetAsync(CancellationToken cancellationToken = default);
    Task<object> TestCacheAsync(CancellationToken cancellationToken = default);
    Task<object> TestMessagingAsync(string? correlationId, CancellationToken cancellationToken = default);
}
