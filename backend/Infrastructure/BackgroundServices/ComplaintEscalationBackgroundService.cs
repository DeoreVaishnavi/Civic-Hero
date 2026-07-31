
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Infrastructure.Configurations;
using CivicHero.Backend.Infrastructure.Data;
using CivicHero.Backend.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Infrastructure.BackgroundServices;

public sealed class ComplaintEscalationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AutomationOptions _options;
    private readonly ILogger<ComplaintEscalationBackgroundService> _logger;
    public ComplaintEscalationBackgroundService(IServiceScopeFactory scopeFactory, IOptions<AutomationOptions> options, ILogger<ComplaintEscalationBackgroundService> logger)
    { _scopeFactory = scopeFactory; _options = options.Value; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled) return;
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        do { await EscalateAsync(stoppingToken); } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task EscalateAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<CivicDbContext>();
            var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
            var cutoff = DateTimeOffset.UtcNow.AddHours(-Math.Clamp(_options.EscalationGraceHours, 1, 168));
            var complaints = await db.Complaints
                .Where(x => x.Status == ComplaintStatus.Escalated && x.UpdatedAt < cutoff && x.Priority != ComplaintPriority.Critical)
                .OrderBy(x => x.UpdatedAt)
                .Take(Math.Clamp(_options.BatchSize, 1, 500))
                .ToListAsync(cancellationToken);
            foreach (var complaint in complaints)
            {
                complaint.Priority = ComplaintPriority.Critical;
                db.ComplaintTimelines.Add(new ComplaintTimeline { ComplaintId = complaint.Id, EventType = "ADMIN_ESCALATION", Description = "The complaint remained overdue after the escalation grace period and was raised to Critical priority.", Timestamp = DateTimeOffset.UtcNow });
                await publisher.PublishAsync("complaint.admin-escalated", new { complaint.Id, complaint.DepartmentId, complaint.WardId }, cancellationToken: cancellationToken);
            }
            if (complaints.Count > 0) await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) { _logger.LogError(exception, "Complaint escalation iteration failed."); }
    }
}
