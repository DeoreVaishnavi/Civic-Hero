using CivicHero.Backend.Core.DTOs.Disputes;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace CivicHero.Backend.Core.Services;
public sealed class DisputeService : IDisputeService
{
 private readonly CivicDbContext _db; private readonly ICurrentUserService _current;
 public DisputeService(CivicDbContext db,ICurrentUserService current){_db=db;_current=current;}
 public async Task<IReadOnlyList<DisputeResponse>> MineAsync(CancellationToken ct=default){EnsureCitizen();return (await Query().Where(x=>x.RaisedByUserId==RequireUser()).OrderByDescending(x=>x.RaisedAt).ToListAsync(ct)).Select(Map).ToArray();}
 public async Task<IReadOnlyList<DisputeResponse>> QueueAsync(bool appealsOnly,CancellationToken ct=default){EnsureReviewRole();var q=ApplyScope(Query());q=appealsOnly?q.Where(x=>x.Status==DisputeStatus.Appealed||x.Status==DisputeStatus.UnderAdminReview):q.Where(x=>x.Status==DisputeStatus.Raised||x.Status==DisputeStatus.UnderSupervisorReview);return (await q.OrderBy(x=>x.RaisedAt).Take(200).ToListAsync(ct)).Select(Map).ToArray();}
 public async Task<DisputeResponse> GetAsync(long id,CancellationToken ct=default){var x=await Query().SingleOrDefaultAsync(v=>v.Id==id,ct)??throw new NotFoundException("Dispute was not found.");EnsureCanView(x);return Map(x);}
 public async Task<DisputeResponse> RaiseAsync(long complaintId,RaiseDisputeRequest r,CancellationToken ct=default){EnsureCitizen();var c=await _db.Complaints.Include(x=>x.Timeline).SingleOrDefaultAsync(x=>x.Id==complaintId&&x.CitizenId==RequireUser(),ct)??throw new NotFoundException("Complaint was not found.");if(c.Status!=ComplaintStatus.VerificationPending&&c.Status!=ComplaintStatus.Resolved)throw new BusinessRuleViolationException("Only a resolved complaint awaiting verification can be disputed.");var now=DateTimeOffset.UtcNow;var item=new DisputeAuditLog{ComplaintId=complaintId,RaisedByUserId=RequireUser(),Status=DisputeStatus.UnderSupervisorReview,CycleNumber=await _db.DisputeAuditLogs.CountAsync(x=>x.ComplaintId==complaintId,ct)+1,CitizenRemarks=r.Reason.Trim(),RaisedAt=now};_db.Add(item);c.Status=ComplaintStatus.Disputed;c.Timeline.Add(T(c.Id,"DISPUTE_RAISED","Citizen raised a dispute: "+r.Reason.Trim()));await _db.SaveChangesAsync(ct);return await GetAsync(item.Id,ct);}
 public async Task<DisputeResponse> SupervisorDecisionAsync(long id,DisputeDecisionRequest r,CancellationToken ct=default){EnsureSupervisor();var x=await Query(true).SingleOrDefaultAsync(v=>v.Id==id,ct)??throw new NotFoundException("Dispute was not found.");EnsureCanView(x);if(x.Status!=DisputeStatus.UnderSupervisorReview&&x.Status!=DisputeStatus.Raised)throw new BusinessRuleViolationException("This dispute is not awaiting supervisor review.");var d=r.Decision.Trim();x.ReviewedByUserId=RequireUser();x.ReviewedAt=DateTimeOffset.UtcNow;x.SupervisorDecision=d;x.SupervisorRemarks=r.Remarks.Trim();if(d.Equals("Rework",StringComparison.OrdinalIgnoreCase)){x.Status=DisputeStatus.ReworkOrdered;x.Complaint.Status=ComplaintStatus.InProgress;x.Complaint.Timeline.Add(T(x.ComplaintId,"REWORK_ORDERED","Supervisor ordered rework: "+r.Remarks.Trim()));}else{x.Status=DisputeStatus.Closed;x.ResolvedAt=DateTimeOffset.UtcNow;x.AppealDeadline=DateTimeOffset.UtcNow.AddDays(7);x.Complaint.Status=ComplaintStatus.Closed;x.Complaint.ClosedAt=DateTimeOffset.UtcNow;x.Complaint.Timeline.Add(T(x.ComplaintId,"DISPUTE_DECIDED","Supervisor "+d+": "+r.Remarks.Trim()));}await _db.SaveChangesAsync(ct);return Map(x);}
 public async Task<DisputeResponse> AppealAsync(long id,AppealDisputeRequest r,CancellationToken ct=default){EnsureCitizen();var x=await Query(true).SingleOrDefaultAsync(v=>v.Id==id&&v.RaisedByUserId==RequireUser(),ct)??throw new NotFoundException("Dispute was not found.");if(x.Status!=DisputeStatus.Closed||!x.AppealDeadline.HasValue||x.AppealDeadline<DateTimeOffset.UtcNow)throw new BusinessRuleViolationException("The appeal window is closed.");x.Status=DisputeStatus.UnderAdminReview;x.AdminRemarks="Citizen appeal: "+r.Remarks.Trim();x.Complaint.Status=ComplaintStatus.Appealed;x.Complaint.Timeline.Add(T(x.ComplaintId,"DISPUTE_APPEALED",r.Remarks.Trim()));await _db.SaveChangesAsync(ct);return Map(x);}
 public async Task<DisputeResponse> AdminDecisionAsync(long id,DisputeDecisionRequest r,CancellationToken ct=default){EnsureAdmin();var x=await Query(true).SingleOrDefaultAsync(v=>v.Id==id,ct)??throw new NotFoundException("Dispute was not found.");if(x.Status!=DisputeStatus.UnderAdminReview&&x.Status!=DisputeStatus.Appealed)throw new BusinessRuleViolationException("This dispute is not awaiting Admin review.");var d=r.Decision.Trim();x.ReviewedByUserId=RequireUser();x.AdminDecision=d;x.AdminRemarks=r.Remarks.Trim();x.ReviewedAt=DateTimeOffset.UtcNow;x.ResolvedAt=DateTimeOffset.UtcNow;x.Status=DisputeStatus.Resolved;if(d.Equals("Rework",StringComparison.OrdinalIgnoreCase)){x.Complaint.Status=ComplaintStatus.InProgress;x.Complaint.ClosedAt=null;}else if(d.Equals("Fraud",StringComparison.OrdinalIgnoreCase)){x.Complaint.Status=ComplaintStatus.ClosedFraud;x.Complaint.ClosedAt=DateTimeOffset.UtcNow;}else{x.Complaint.Status=ComplaintStatus.Closed;x.Complaint.ClosedAt=DateTimeOffset.UtcNow;}x.Complaint.Timeline.Add(T(x.ComplaintId,"ADMIN_DISPUTE_DECISION","Admin "+d+": "+r.Remarks.Trim()));await _db.SaveChangesAsync(ct);return Map(x);}
 private IQueryable<DisputeAuditLog> Query(bool track = false)
 {
     var query = _db.DisputeAuditLogs
         .Include(x => x.Complaint)
             .ThenInclude(x => x.Department)
         .Include(x => x.Complaint)
             .ThenInclude(x => x.Ward);

     return track
         ? query.AsTracking()
         : query.AsNoTracking();
 }
 private IQueryable<DisputeAuditLog> ApplyScope(IQueryable<DisputeAuditLog> q){if(string.Equals(_current.Role,"Supervisor",StringComparison.OrdinalIgnoreCase)){q=q.Where(x=>x.Complaint.DepartmentId==_current.DepartmentId);if(_current.WardId.HasValue)q=q.Where(x=>x.Complaint.WardId==_current.WardId);}return q;}
 private void EnsureCanView(DisputeAuditLog x){if(string.Equals(_current.Role,"Citizen",StringComparison.OrdinalIgnoreCase)&&x.RaisedByUserId!=RequireUser())throw new NotFoundException("Dispute was not found.");if(string.Equals(_current.Role,"Supervisor",StringComparison.OrdinalIgnoreCase)&&(x.Complaint.DepartmentId!=_current.DepartmentId||(_current.WardId.HasValue&&x.Complaint.WardId!=_current.WardId)))throw new NotFoundException("Dispute was not found.");}
 private static DisputeResponse Map(DisputeAuditLog x)=>new(x.Id,x.ComplaintId,"CH-"+x.ComplaintId.ToString("D6"),x.Complaint.Title,x.Status.ToString(),x.CycleNumber,x.CitizenRemarks,x.SupervisorDecision,x.SupervisorRemarks,x.AdminDecision,x.AdminRemarks,x.RaisedAt,x.AppealDeadline,x.Status==DisputeStatus.Closed&&x.AppealDeadline>DateTimeOffset.UtcNow);
 private ComplaintTimeline T(long id,string type,string text)=>new(){ComplaintId=id,UserId=_current.UserId,EventType=type,Description=text,Timestamp=DateTimeOffset.UtcNow};
 private long RequireUser()=>_current.UserId??throw new BusinessRuleViolationException("Authenticated user is required.");
 private void EnsureCitizen(){if(!string.Equals(_current.Role,"Citizen",StringComparison.OrdinalIgnoreCase))throw new BusinessRuleViolationException("Citizen access is required.");}
 private void EnsureSupervisor(){if(!new[]{"Supervisor","Admin","SuperAdmin"}.Contains(_current.Role,StringComparer.OrdinalIgnoreCase))throw new BusinessRuleViolationException("Supervisor access is required.");}
 private void EnsureAdmin(){if(!new[]{"Admin","SuperAdmin"}.Contains(_current.Role,StringComparer.OrdinalIgnoreCase))throw new BusinessRuleViolationException("Admin access is required.");}
 private void EnsureReviewRole(){if(!new[]{"Supervisor","Admin","SuperAdmin"}.Contains(_current.Role,StringComparer.OrdinalIgnoreCase))throw new BusinessRuleViolationException("Review access is required.");}
}

