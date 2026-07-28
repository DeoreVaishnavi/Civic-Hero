using CivicHero.Backend.Core.DTOs.Disputes;
using CivicHero.Backend.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DisputesController : ControllerBase
    {
        private readonly IDisputeManagementService _disputeService;

        public DisputesController(IDisputeManagementService disputeService)
        {
            _disputeService = disputeService;
        }

        // GET: api/disputes
        [HttpGet]
        public async Task<ActionResult<DisputeRowDto[]>> GetDisputes(int? complaintId = null, int? userId = null, string? status = null)
        {
            var disputes = await _disputeService.GetDisputesAsync(complaintId, userId, status);
            return Ok(disputes);
        }

        // GET: api/disputes/{complaintId}/audit
        [HttpGet("{complaintId}/audit")]
        public async Task<ActionResult<DisputeAuditLogRowDto[]>> GetAuditLog(int complaintId)
        {
            var auditLog = await _disputeService.GetAuditLogAsync(complaintId);
            return Ok(auditLog);
        }

        // POST: api/disputes/initiate
        [HttpPost("initiate")]
        public async Task<ActionResult<DisputeAuditLogRowDto>> InitiateDispute([FromBody] InitiateDisputeDto initiateDisputeDto)
        {
            var result = await _disputeService.InitiateDisputeAsync(
                initiateDisputeDto.ComplaintId,
                initiateDisputeDto.InitiatedByUserId,
                initiateDisputeDto.Details ?? "");
            return Ok(result);
        }

        // POST: api/disputes/evidence
        [HttpPost("evidence")]
        public async Task<ActionResult<DisputeAuditLogRowDto>> AddEvidence([FromBody] AddEvidenceDto addEvidenceDto)
        {
            var result = await _disputeService.AddEvidenceAsync(
                addEvidenceDto.ComplaintId,
                addEvidenceDto.UserId,
                addEvidenceDto.EvidenceDescription ?? "");
            return Ok(result);
        }

        // POST: api/disputes/{complaintId}/verdict/{userId}
        [HttpPost("{complaintId}/verdict/{userId}")]
        public async Task<ActionResult<DisputeAuditLogRowDto>> SubmitVerdict(int complaintId, int userId, [FromBody] VerdictSubmissionDto verdictSubmission)
        {
            var result = await _disputeService.SubmitVerdictAsync(
                complaintId,
                userId,
                verdictSubmission);
            return Ok(result);
        }

        // POST: api/disputes/{complaintId}/reanalyze/{userId}
        [HttpPost("{complaintId}/reanalyze/{userId}")]
        public async Task<ActionResult<DisputeAuditLogRowDto>> RequestReAnalysis(int complaintId, int userId, [FromBody] ReAnalyzeRequest reAnalyzeRequest)
        {
            var result = await _disputeService.RequestReAnalysisAsync(
                complaintId,
                userId,
                reAnalyzeRequest);
            return Ok(result);
        }
    }
}