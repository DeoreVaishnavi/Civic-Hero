
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Infrastructure.Configurations;
using CivicHero.Backend.Infrastructure.Data;
using CivicHero.Backend.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Infrastructure.BackgroundServices;

public sealed class AutoCloseBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AutomationOptions _options;
    private readonly ILogger<AutoCloseBackgroundService> _logger;
    public AutoCloseBackgroundService(IServiceScopeFactory scopeFactory, IOptions<AutomationOptions> options, ILogger<AutoCloseBackgroundService> logger)
    { _scopeFactory = scopeFactory; _options = options.Value; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled) return;
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        do { await CloseExpiredAsync(stoppingToken); } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CloseExpiredAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<CivicDbContext>();
            var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
            var now = DateTimeOffset.UtcNow;
            var candidates = await db.Complaints
                .Where(x => x.Status == ComplaintStatus.VerificationPending && x.ResolvedAt != null)
                .OrderBy(x => x.ResolvedAt)
                .Take(Math.Clamp(_options.BatchSize, 1, 500))
                .ToListAsync(cancellationToken);
            foreach (var complaint in candidates)
            {
                var hours = complaint.Priority switch
                {
                    ComplaintPriority.Critical => _options.CriticalVerificationHours,
                    ComplaintPriority.High => _options.HighVerificationHours,
                    _ => _options.MediumVerificationHours
                };
                if (complaint.ResolvedAt!.Value.AddHours(Math.Clamp(hours, 1, 168)) > now) continue;
                complaint.Status = ComplaintStatus.ClosedAuto;
                complaint.ClosedAt = now;
                db.ComplaintTimelines.Add(new ComplaintTimeline { ComplaintId = complaint.Id, EventType = "AUTO_CLOSED", Description = "Citizen verification window expired; complaint closed automatically.", Timestamp = now });
                await publisher.PublishAsync("complaint.auto-closed", new { complaint.Id, complaint.CitizenId, complaint.ClosedAt }, cancellationToken: cancellationToken);
            }
            if (candidates.Any(x => x.Status == ComplaintStatus.ClosedAuto)) await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) { _logger.LogError(exception, "Auto-close iteration failed."); }
    }
}
