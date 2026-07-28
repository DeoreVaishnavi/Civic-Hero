using CivicHero.Backend.Core.DTOs.Disputes;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.AI;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CivicHero.Backend.Core.Services
{
    public class DisputeManagementService : IDisputeManagementService
    {
        private readonly CivicHeroDbContext _context;
        private readonly IReputationService _reputationService;
        private readonly IFraudAnalysisService _fraudAnalysisService;

        public DisputeManagementService(
            CivicHeroDbContext context,
            IReputationService reputationService,
            IFraudAnalysisService fraudAnalysisService)
        {
            _context = context;
            _reputationService = reputationService;
            _fraudAnalysisService = fraudAnalysisService;
        }

        public async Task<DisputeAuditLogRowDto> InitiateDisputeAsync(int complaintId, int initiatedByUserId, string details = "")
        {
            // Validate that the complaint exists? We don't have a Complaint entity, but we can assume it's validated elsewhere.
            // For now, we'll just create the audit log.

            var auditLog = new DisputeAuditLog
            {
                ComplaintId = complaintId,
                InitiatedByUserId = initiatedByUserId,
                Action = "Dispute initiated",
                Details = details,
                Timestamp = DateTime.UtcNow
            };

            _context.DisputeAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            return new DisputeAuditLogRowDto
            {
                Id = auditLog.Id,
                ComplaintId = auditLog.ComplaintId,
                UserName = $"User {auditLog.InitiatedByUserId}", // In a real app, we'd fetch the user's name
                Action = auditLog.Action,
                Details = auditLog.Details,
                Timestamp = auditLog.Timestamp
            };
        }

        public async Task<DisputeAuditLogRowDto> AddEvidenceAsync(int complaintId, int userId, string evidenceDetails)
        {
            var auditLog = new DisputeAuditLog
            {
                ComplaintId = complaintId,
                InitiatedByUserId = userId,
                Action = "Evidence submitted",
                Details = evidenceDetails,
                Timestamp = DateTime.UtcNow
            };

            _context.DisputeAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            return new DisputeAuditLogRowDto
            {
                Id = auditLog.Id,
                ComplaintId = auditLog.ComplaintId,
                UserName = $"User {auditLog.InitiatedByUserId}",
                Action = auditLog.Action,
                Details = auditLog.Details,
                Timestamp = auditLog.Timestamp
            };
        }

        public async Task<DisputeAuditLogRowDto> SubmitVerdictAsync(int complaintId, int userId, VerdictSubmissionDto verdictSubmission)
        {
            // Create audit log for verdict submission
            var auditLog = new DisputeAuditLog
            {
                ComplaintId = complaintId,
                InitiatedByUserId = userId,
                Action = "Verdict submitted",
                Details = $"Verdict: {verdictSubmission.Verdict}, Reasoning: {verdictSubmission.Reasoning}",
                Timestamp = DateTime.UtcNow
            };

            _context.DisputeAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            // If points are awarded or deducted, update the user's reputation
            // Note: The VerdictSubmissionDto has PointsAwarded and PointsDeducted.
            // We assume that only one of them is set, or we handle both.
            if (verdictSubmission.PointsAwarded.HasValue && verdictSubmission.PointsAwarded.Value > 0)
            {
                await _reputationService.AddPointsAsync(
                    userId,
                    verdictSubmission.PointsAwarded.Value,
                    $"Points awarded for verdict on complaint {complaintId}: {verdictSubmission.Verdict}");
            }

            if (verdictSubmission.PointsDeducted.HasValue && verdictSubmission.PointsDeducted.Value > 0)
            {
                await _reputationService.DeductPointsAsync(
                    userId,
                    verdictSubmission.PointsDeducted.Value,
                    $"Points deducted for verdict on complaint {complaintId}: {verdictSubmission.Verdict}");
            }

            return new DisputeAuditLogRowDto
            {
                Id = auditLog.Id,
                ComplaintId = auditLog.ComplaintId,
                UserName = $"User {auditLog.InitiatedByUserId}",
                Action = auditLog.Action,
                Details = auditLog.Details,
                Timestamp = auditLog.Timestamp
            };
        }

        public async Task<DisputeAuditLogRowDto> RequestReAnalysisAsync(int complaintId, int userId, ReAnalyzeRequest reAnalyzeRequest)
        {
            // Create audit log for re-analysis request
            var auditLog = new DisputeAuditLog
            {
                ComplaintId = complaintId,
                InitiatedByUserId = userId,
                Action = "Re-analysis requested",
                Details = reAnalyzeRequest.Reason,
                Timestamp = DateTime.UtcNow
            };

            _context.DisputeAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            // Optionally, trigger a re-analysis of the complaint via the fraud analysis service
            // This would depend on having a method in IFraudAnalysisService to re-analyze a complaint by ID.
            // Since we don't see such a method in the existing IFraudAnalysisService, we might need to extend it.
            // For now, we'll just log the request and leave the actual re-analysis to be implemented elsewhere.
            // However, let's assume we have a method to trigger re-analysis for a complaint.
            // If not, we can comment out or throw a not implemented exception.

            // Example: await _fraudAnalysisService.ReAnalyzeComplaintAsync(complaintId);

            return new DisputeAuditLogRowDto
            {
                Id = auditLog.Id,
                ComplaintId = auditLog.ComplaintId,
                UserName = $"User {auditLog.InitiatedByUserId}",
                Action = auditLog.Action,
                Details = auditLog.Details,
                Timestamp = auditLog.Timestamp
            };
        }

        public Task<DisputeRowDto[]> GetDisputesAsync(int? complaintId = null, int? userId = null, string? status = null)
        {
            // We don't have a Dispute entity, so we'll have to construct the DisputeRowDto from the audit logs and possibly other sources.
            // However, the DisputeRowDto seems to represent a dispute case (complaint) with some summary information.
            // Since we don't have a Complaint entity in the provided code path.
            // We'll note that the DisputeRowDto requires ComplainantName and ComplainantEmail, which we don't have in our current entities.
            // This suggests that there might be a Complaint entity or a User relationship that we are missing.

            // Given the constraints, we will return an empty array for now, but note that this method needs to be implemented
            // once we have access to complaint and user details.

            // For the sake of the task, we'll return an empty array and note that the implementation requires additional data.
            return Task.FromResult(Array.Empty<DisputeRowDto>());
        }

        public async Task<DisputeAuditLogRowDto[]> GetAuditLogAsync(int complaintId)
        {
            var auditLogs = await _context.DisputeAuditLogs
                .Where(dal => dal.ComplaintId == complaintId)
                .OrderByDescending(dal => dal.Timestamp)
                .ToListAsync();

            return auditLogs.Select(dal => new DisputeAuditLogRowDto
            {
                Id = dal.Id,
                ComplaintId = dal.ComplaintId,
                UserName = $"User {dal.InitiatedByUserId}", // Again, ideally we'd get the actual username
                Action = dal.Action,
                Details = dal.Details,
                Timestamp = dal.Timestamp
            }).ToArray();
        }
    }
}