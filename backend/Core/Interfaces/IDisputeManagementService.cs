using CivicHero.Backend.Core.DTOs.Disputes;
using System.Threading.Tasks;

namespace CivicHero.Backend.Core.Interfaces
{
    public interface IDisputeManagementService
    {
        Task<DisputeAuditLogRowDto> InitiateDisputeAsync(int complaintId, int initiatedByUserId, string details = "");
        Task<DisputeAuditLogRowDto> AddEvidenceAsync(int complaintId, int userId, string evidenceDetails);
        Task<DisputeAuditLogRowDto> SubmitVerdictAsync(int complaintId, int userId, VerdictSubmissionDto verdictSubmission);
        Task<DisputeAuditLogRowDto> RequestReAnalysisAsync(int complaintId, int userId, ReAnalyzeRequest reAnalyzeRequest);
        Task<DisputeRowDto[]> GetDisputesAsync(int? complaintId = null, int? userId = null, string? status = null);
        Task<DisputeAuditLogRowDto[]> GetAuditLogAsync(int complaintId);
    }
}