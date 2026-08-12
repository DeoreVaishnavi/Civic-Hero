using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace CivicHero.Backend.Infrastructure.BackgroundServices;
public sealed class VerificationTimeoutWorker : BackgroundService
{
 private readonly IServiceScopeFactory _scopeFactory; private readonly ILogger<VerificationTimeoutWorker> _logger;
 public VerificationTimeoutWorker(IServiceScopeFactory scopeFactory,ILogger<VerificationTimeoutWorker> logger){_scopeFactory=scopeFactory;_logger=logger;}
 protected override async Task ExecuteAsync(CancellationToken stoppingToken){await Task.Delay(TimeSpan.FromSeconds(20),stoppingToken);while(!stoppingToken.IsCancellationRequested){try{await Process(stoppingToken);}catch(Exception ex){_logger.LogWarning(ex,"Verification timeout processing skipped because the database is unavailable.");}await Task.Delay(TimeSpan.FromMinutes(5),stoppingToken);}}
 private async Task Process(CancellationToken ct){await using var scope=_scopeFactory.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<CivicDbContext>();var now=DateTimeOffset.UtcNow;var rows=await db.ComplaintVerifications.Include(x=>x.Complaint).ThenInclude(x=>x.Timeline).Where(x=>x.Decision==VerificationDecision.Pending&&x.DueAt<=now&&x.Complaint.Status==ComplaintStatus.VerificationPending).Take(100).ToListAsync(ct);foreach(var x in rows){x.Decision=VerificationDecision.AutoClosed;x.CompletedAt=now;x.Complaint.Status=ComplaintStatus.ClosedAuto;x.Complaint.ClosedAt=now;x.Complaint.Timeline.Add(new ComplaintTimeline{ComplaintId=x.ComplaintId,EventType="VERIFICATION_TIMEOUT",Description="Verification window expired; complaint auto-closed.",Timestamp=now});}if(rows.Count>0)await db.SaveChangesAsync(ct);}
}
