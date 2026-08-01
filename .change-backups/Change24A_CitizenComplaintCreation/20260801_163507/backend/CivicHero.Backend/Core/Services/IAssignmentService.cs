using CivicHero.Backend.Core.DTOs.Assignments;
using CivicHero.Backend.Core.DTOs.Common;

namespace CivicHero.Backend.Core.Services;

public interface IAssignmentService
{
    Task<AssignmentDto> AssignAsync(AssignComplaintRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssignmentDto>> BulkAssignAsync(BulkAssignRequest request, CancellationToken cancellationToken = default);
    Task<AssignmentDto> ReassignAsync(long complaintId, ReassignComplaintRequest request, CancellationToken cancellationToken = default);
    Task<AssignmentDto> AcceptAsync(long complaintId, CancellationToken cancellationToken = default);
    Task<AssignmentDto> RejectAsync(long complaintId, RejectAssignmentRequest request, CancellationToken cancellationToken = default);
    Task<AssignmentDto> AddProgressAsync(long complaintId, AddProgressRequest request, CancellationToken cancellationToken = default);
    Task<AssignmentDto> CompleteAsync(long complaintId, CompleteAssignmentRequest request, CancellationToken cancellationToken = default);
    Task<AssignmentDto> EscalateAsync(long complaintId, SupervisorActionRequest request, CancellationToken cancellationToken = default);
    Task<AssignmentDto> ResumeEscalatedAsync(long complaintId, SupervisorActionRequest request, CancellationToken cancellationToken = default);
    Task<AssignmentDto> RequestReworkAsync(long complaintId, SupervisorActionRequest request, CancellationToken cancellationToken = default);
    Task<AssignmentDto> SendOfficerInstructionAsync(long complaintId, SupervisorMessageRequest request, CancellationToken cancellationToken = default);
    Task<AssignmentDto> RequestCitizenEvidenceAsync(long complaintId, SupervisorMessageRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EscalationHistoryDto>> GetEscalationHistoryAsync(AssignmentQuery query, CancellationToken cancellationToken = default);
    Task<PagedResponse<AssignmentDto>> GetMyAssignmentsAsync(AssignmentQuery query, CancellationToken cancellationToken = default);
    Task<PagedResponse<AssignmentDto>> GetPendingAsync(AssignmentQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssignmentDto>> GetOverdueAsync(CancellationToken cancellationToken = default);
    Task<AssignmentDto> GetByComplaintIdAsync(long complaintId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssignmentHistoryDto>> GetHistoryAsync(long complaintId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OfficerWorkloadDto>> GetWorkloadAsync(long? departmentId, long? wardId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EligibleOfficerDto>> GetEligibleOfficersAsync(long complaintId, CancellationToken cancellationToken = default);
    Task<AssignmentDashboardResponse> GetOfficerDashboardAsync(CancellationToken cancellationToken = default);
    Task<AssignmentDashboardResponse> GetSupervisorDashboardAsync(CancellationToken cancellationToken = default);
}
