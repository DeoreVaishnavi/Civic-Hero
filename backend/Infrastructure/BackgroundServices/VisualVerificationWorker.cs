using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Services;
using CivicHero.Backend.Infrastructure.Configurations;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Infrastructure.BackgroundServices;

public sealed class VisualVerificationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<VisualVerificationOptions> _options;
    private readonly ILogger<VisualVerificationWorker> _logger;

    public VisualVerificationWorker(IServiceScopeFactory scopeFactory, IOptionsMonitor<VisualVerificationOptions> options, ILogger<VisualVerificationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var options = _options.CurrentValue;
            if (options.Enabled)
            {
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var db = scope.ServiceProvider.GetRequiredService<CivicDbContext>();
                    var service = scope.ServiceProvider.GetRequiredService<IVisualVerificationService>();
                    var ids = await db.Complaints.AsNoTracking()
                        .Where(entity => entity.Status == ComplaintStatus.VerificationPending &&
                                         entity.Images.Any(image => !image.IsResolutionEvidence) &&
                                         entity.Images.Any(image => image.IsResolutionEvidence) &&
                                         (!entity.VisualVerificationAnalyses.Any() ||
                                          entity.Images.Any(image => image.IsResolutionEvidence &&
                                              !entity.VisualVerificationAnalyses.Any(analysis => analysis.CreatedAt >= image.UploadedAt))))
                        .OrderBy(entity => entity.ResolvedAt)
                        .Select(entity => entity.Id)
                        .Take(Math.Clamp(options.BatchSize, 1, 50))
                        .ToListAsync(stoppingToken);
                    foreach (var id in ids)
                    {
                        try { await service.AnalyzeComplaintAsync(id, stoppingToken); }
                        catch (Exception exception) { _logger.LogWarning(exception, "Visual verification failed for complaint {ComplaintId}.", id); }
                    }
                }
                catch (Exception exception) { _logger.LogError(exception, "Visual verification worker cycle failed."); }
            }
            await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(options.WorkerIntervalSeconds, 30, 3600)), stoppingToken);
        }
    }
}
