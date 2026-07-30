using CivicHero.Backend.Core.Services;
using CivicHero.Backend.Infrastructure.Configurations;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Infrastructure.BackgroundServices;

public sealed class RewardAwardWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RewardAwardWorker> _logger;
    private readonly RewardsOptions _options;

    public RewardAwardWorker(IServiceScopeFactory scopeFactory, ILogger<RewardAwardWorker> logger, IOptions<RewardsOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Clamp(_options.AwardWorkerIntervalSeconds, 30, 3600)));
        do
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var count = await scope.ServiceProvider.GetRequiredService<IRewardService>().ProcessEligibleAwardsAsync(stoppingToken);
                if (count > 0) _logger.LogInformation("Awarded CivicHero points for {Count} newly closed complaints.", count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Reward award worker failed. It will retry on the next cycle.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
