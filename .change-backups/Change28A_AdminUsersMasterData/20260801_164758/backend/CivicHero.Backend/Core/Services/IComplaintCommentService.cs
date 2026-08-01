using CivicHero.Backend.Core.DTOs.Comments;
using CivicHero.Backend.Core.DTOs.Common;

namespace CivicHero.Backend.Core.Services;

public interface IComplaintCommentService
{
    Task<IReadOnlyList<ComplaintCommentResponse>> GetAsync(long complaintId, CancellationToken cancellationToken = default);
    Task<PagedResponse<ComplaintCommentModerationItemResponse>> GetModerationQueueAsync(ComplaintCommentModerationQuery query, CancellationToken cancellationToken = default);
    Task<ComplaintCommentResponse> AddAsync(long complaintId, AddComplaintCommentRequest request, CancellationToken cancellationToken = default);
    Task<ComplaintCommentResponse> UpdateAsync(long complaintId, long commentId, UpdateComplaintCommentRequest request, CancellationToken cancellationToken = default);
    Task<ComplaintCommentReportResponse> ReportAsync(long complaintId, long commentId, ReportComplaintCommentRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(long complaintId, long commentId, CancellationToken cancellationToken = default);
    Task<ComplaintCommentResponse> ModerateAsync(long complaintId, long commentId, ModerateComplaintCommentRequest request, CancellationToken cancellationToken = default);
}
