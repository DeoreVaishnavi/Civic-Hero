using CivicHero.Backend.Core.DTOs.Verification;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace CivicHero.Backend.Core.Services;
public sealed class VerificationService : IVerificationService
{
 private readonly CivicDbContext _db; private readonly ICurrentUserService _current;
 public VerificationService(CivicDbContext db, ICurrentUserService current){_db=db;_current=current;}
 public async Task<IReadOnlyList<VerificationQueueItem>> GetPendingAsync(CancellationToken ct=default){EnsureRole("Citizen"); var uid=RequireUser(); await EnsureRowsAsync(ct); return await QueryQueue().Where(x=>x.Complaint.CitizenId==uid && x.Decision==VerificationDecision.Pending).OrderBy(x=>x.DueAt).Select(x=>new VerificationQueueItem(x.ComplaintId,"CH-"+x.ComplaintId.ToString("D6"),x.Complaint.Title,x.Complaint.Priority.ToString(),x.Complaint.Department.Name,x.Complaint.Ward.Name,x.DueAt,(long)(x.DueAt-DateTimeOffset.UtcNow).TotalMinutes,x.DueAt<DateTimeOffset.UtcNow)).ToListAsync(ct);}
 public async Task<IReadOnlyList<VerificationQueueItem>> GetSupervisorQueueAsync(bool overdueOnly,CancellationToken ct=default){EnsureSupervisor(); await EnsureRowsAsync(ct); var q=ApplyScope(QueryQueue().Where(x=>x.Decision==VerificationDecision.Pending)); if(overdueOnly)q=q.Where(x=>x.DueAt<DateTimeOffset.UtcNow); return await q.OrderBy(x=>x.DueAt).Select(x=>new VerificationQueueItem(x.ComplaintId,"CH-"+x.ComplaintId.ToString("D6"),x.Complaint.Title,x.Complaint.Priority.ToString(),x.Complaint.Department.Name,x.Complaint.Ward.Name,x.DueAt,(long)(x.DueAt-DateTimeOffset.UtcNow).TotalMinutes,x.DueAt<DateTimeOffset.UtcNow)).Take(200).ToListAsync(ct);}
 public async Task<VerificationResponse> GetAsync(long id,CancellationToken ct=default){await EnsureRowsAsync(ct); var row=await QueryQueue().SingleOrDefaultAsync(x=>x.ComplaintId==id,ct)??throw new NotFoundException("Verification was not found."); EnsureCanView(row); return Map(row);}
 public async Task<object> CheckGeoAsync(long id,GeoVerifyRequest r,CancellationToken ct=default){var row=await LoadCitizenAsync(id,ct); var d=Distance((double)row.Complaint.Latitude,(double)row.Complaint.Longitude,(double)r.Latitude,(double)r.Longitude); return new{distanceMetres=Math.Round(d,1),withinAllowedRange=d<=500,allowedDistanceMetres=500};}
 public async Task<VerificationResponse> VerifyAsync(long id,VerifyComplaintRequest r,CancellationToken ct=default){var row=await LoadCitizenAsync(id,ct); if(row.Decision!=VerificationDecision.Pending||row.Complaint.Status!=ComplaintStatus.VerificationPending)throw new BusinessRuleViolationException("This complaint is no longer awaiting verification."); var d=Distance((double)row.Complaint.Latitude,(double)row.Complaint.Longitude,(double)r.Latitude,(double)r.Longitude); if(d>500)throw new BusinessRuleViolationException($"Move within 500 metres of the complaint. Current distance: {Math.Round(d)} metres."); var now=DateTimeOffset.UtcNow; row.Rating=r.Rating; row.Remarks=r.Remarks?.Trim(); row.SubmittedLatitude=r.Latitude; row.SubmittedLongitude=r.Longitude; row.DistanceMetres=d; row.CompletedAt=now; if(r.Approved){row.Decision=VerificationDecision.Approved; row.Complaint.Status=ComplaintStatus.Closed; row.Complaint.ClosedAt=now; AddTimeline(row.Complaint,"CITIZEN_VERIFIED","Citizen approved the resolution with rating "+r.Rating+"/5.");} else {row.Decision=VerificationDecision.Rejected; row.Complaint.Status=ComplaintStatus.Disputed; var cycle=await _db.DisputeAuditLogs.CountAsync(x=>x.ComplaintId==id,ct)+1; _db.DisputeAuditLogs.Add(new DisputeAuditLog{ComplaintId=id,RaisedByUserId=RequireUser(),Status=DisputeStatus.UnderSupervisorReview,CycleNumber=cycle,CitizenRemarks=string.IsNullOrWhiteSpace(r.Remarks)?"Citizen rejected the resolution.":r.Remarks.Trim(),RaisedAt=now}); AddTimeline(row.Complaint,"RESOLUTION_REJECTED","Citizen rejected the resolution and opened a dispute.");} await _db.SaveChangesAsync(ct); return Map(row);}
 public async Task<VerificationResponse> RemindAsync(long id,CancellationToken ct=default){EnsureSupervisor(); await EnsureRowsAsync(ct); var row=await QueryQueue(true).SingleOrDefaultAsync(x=>x.ComplaintId==id,ct)??throw new NotFoundException("Verification was not found."); EnsureCanView(row); row.ReminderSentAt=DateTimeOffset.UtcNow; AddTimeline(row.Complaint,"VERIFICATION_REMINDER","A verification reminder was recorded for the citizen."); await _db.SaveChangesAsync(ct); return Map(row);}
 private IQueryable<ComplaintVerification> QueryQueue(bool track = false)
 {
     var query = _db.ComplaintVerifications
         .Include(x => x.Complaint)
             .ThenInclude(x => x.Department)
         .Include(x => x.Complaint)
             .ThenInclude(x => x.Ward);

     return track
         ? query.AsTracking()
         : query.AsNoTracking();
 }
 private async Task<ComplaintVerification> LoadCitizenAsync(long id,CancellationToken ct){EnsureRole("Citizen"); await EnsureRowsAsync(ct); return await QueryQueue(true).SingleOrDefaultAsync(x=>x.ComplaintId==id&&x.CitizenId==RequireUser(),ct)??throw new NotFoundException("Verification was not found.");}
 private async Task EnsureRowsAsync(CancellationToken ct){var existing=await _db.ComplaintVerifications.Select(x=>x.ComplaintId).ToListAsync(ct); var items=await _db.Complaints.Where(x=>x.Status==ComplaintStatus.VerificationPending&&!existing.Contains(x.Id)).ToListAsync(ct); foreach(var c in items)_db.ComplaintVerifications.Add(new ComplaintVerification{ComplaintId=c.Id,CitizenId=c.CitizenId,DueAt=(c.ResolvedAt??DateTimeOffset.UtcNow).AddHours(Window(c.Priority))}); if(items.Count>0)await _db.SaveChangesAsync(ct);}
 private static int Window(ComplaintPriority p)=>p==ComplaintPriority.Critical?24:p==ComplaintPriority.High?48:72;
 private IQueryable<ComplaintVerification> ApplyScope(IQueryable<ComplaintVerification> q){if(string.Equals(_current.Role,"Supervisor",StringComparison.OrdinalIgnoreCase)){if(!_current.DepartmentId.HasValue)throw new BusinessRuleViolationException("Supervisor department scope is missing.");q=q.Where(x=>x.Complaint.DepartmentId==_current.DepartmentId.Value);if(_current.WardId.HasValue)q=q.Where(x=>x.Complaint.WardId==_current.WardId.Value);}return q;}
 private void EnsureCanView(ComplaintVerification r){if(string.Equals(_current.Role,"Citizen",StringComparison.OrdinalIgnoreCase)&&r.CitizenId!=RequireUser())throw new NotFoundException("Verification was not found."); if(string.Equals(_current.Role,"Supervisor",StringComparison.OrdinalIgnoreCase)&&(r.Complaint.DepartmentId!=_current.DepartmentId||(_current.WardId.HasValue&&r.Complaint.WardId!=_current.WardId)))throw new NotFoundException("Verification was not found.");}
 private void EnsureSupervisor(){if(!new[]{"Supervisor","Admin","SuperAdmin"}.Contains(_current.Role,StringComparer.OrdinalIgnoreCase))throw new BusinessRuleViolationException("Supervisor access is required.");}
 private void EnsureRole(string role){if(!string.Equals(_current.Role,role,StringComparison.OrdinalIgnoreCase))throw new BusinessRuleViolationException(role+" access is required.");}
 private long RequireUser()=>_current.UserId??throw new BusinessRuleViolationException("Authenticated user is required.");
 private void AddTimeline(Complaint c,string type,string text)=>c.Timeline.Add(new ComplaintTimeline{ComplaintId=c.Id,UserId=_current.UserId,EventType=type,Description=text,Timestamp=DateTimeOffset.UtcNow});
 private static VerificationResponse Map(ComplaintVerification x)=>new(x.ComplaintId,"CH-"+x.ComplaintId.ToString("D6"),x.Complaint.Title,x.Complaint.Status.ToString(),x.Decision.ToString(),x.Rating,x.Remarks,x.DistanceMetres,x.DueAt,x.CompletedAt,x.Decision==VerificationDecision.Pending&&x.Complaint.Status==ComplaintStatus.VerificationPending,x.Decision==VerificationDecision.Pending);
 private static double Distance(double a,double b,double c,double d){const double R=6371000;double p1=a*Math.PI/180,p2=c*Math.PI/180,dp=(c-a)*Math.PI/180,dl=(d-b)*Math.PI/180;double h=Math.Sin(dp/2)*Math.Sin(dp/2)+Math.Cos(p1)*Math.Cos(p2)*Math.Sin(dl/2)*Math.Sin(dl/2);return R*2*Math.Atan2(Math.Sqrt(h),Math.Sqrt(1-h));}
}


