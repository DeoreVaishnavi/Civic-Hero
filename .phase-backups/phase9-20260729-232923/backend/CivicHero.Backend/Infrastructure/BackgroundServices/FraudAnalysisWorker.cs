using CivicHero.Backend.Core.Services;
using CivicHero.Backend.Infrastructure.Configurations;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Infrastructure.BackgroundServices;

public sealed class FraudAnalysisWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AiOptions _options;
    private readonly ILogger<FraudAnalysisWorker> _logger;

    public FraudAnalysisWorker(IServiceScopeFactory scopeFactory, IOptions<AiOptions> options, ILogger<FraudAnalysisWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableBackgroundTriage)
        {
            _logger.LogInformation("CivicHero AI background triage is disabled.");
            return;
        }

        await ProcessAsync(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Clamp(_options.WorkerIntervalSeconds, 15, 3600)));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await ProcessAsync(stoppingToken);
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IAiTriageService>();
            var count = await service.ProcessPendingAsync(cancellationToken);
            if (count > 0) _logger.LogInformation("CivicHero AI triaged {Count} complaint(s).", count);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            _logger.LogError(exception, "CivicHero AI background triage failed; the next cycle will retry.");
        }
    }
}
