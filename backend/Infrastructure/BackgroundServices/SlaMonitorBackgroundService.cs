
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Infrastructure.Configurations;
using CivicHero.Backend.Infrastructure.Data;
using CivicHero.Backend.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Infrastructure.BackgroundServices;

public sealed class SlaMonitorBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AutomationOptions _options;
    private readonly ILogger<SlaMonitorBackgroundService> _logger;
    public SlaMonitorBackgroundService(IServiceScopeFactory scopeFactory, IOptions<AutomationOptions> options, ILogger<SlaMonitorBackgroundService> logger)
    { _scopeFactory = scopeFactory; _options = options.Value; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled) return;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Clamp(_options.PollIntervalSeconds, 15, 3600)));
        do { await ProcessAsync(stoppingToken); } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<CivicDbContext>();
            var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
            var now = DateTimeOffset.UtcNow;
            var assignments = await db.ComplaintAssignments
                .Include(x => x.Complaint)
                .Where(x => x.IsCurrent && x.Status != AssignmentStatus.Completed && x.Status != AssignmentStatus.Cancelled &&
                    (x.AssignmentDueAt < now || x.ResolutionDueAt < now))
                .OrderBy(x => x.ResolutionDueAt)
                .Take(Math.Clamp(_options.BatchSize, 1, 500))
                .ToListAsync(cancellationToken);
            foreach (var assignment in assignments)
            {
                var complaint = assignment.Complaint;
                if (complaint.Status is ComplaintStatus.Closed or ComplaintStatus.ClosedAuto or ComplaintStatus.ClosedFraud or ComplaintStatus.Withdrawn) continue;
                var oldPriority = complaint.Priority;
                complaint.Status = ComplaintStatus.Escalated;
                complaint.Priority = complaint.Priority switch { ComplaintPriority.Low => ComplaintPriority.Medium, ComplaintPriority.Medium => ComplaintPriority.High, _ => ComplaintPriority.Critical };
                db.ComplaintTimelines.Add(new ComplaintTimeline { ComplaintId = complaint.Id, EventType = "SLA_BREACH", Description = $"SLA breached. Priority changed from {oldPriority} to {complaint.Priority}.", Timestamp = now });
                await publisher.PublishAsync("complaint.sla-breached", new { complaint.Id, complaint.Priority, assignment.OfficerId, assignment.ResolutionDueAt }, cancellationToken: cancellationToken);
            }
            if (assignments.Count > 0) await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) { _logger.LogError(exception, "SLA monitor iteration failed; the next scheduled run will retry."); }
    }
}
