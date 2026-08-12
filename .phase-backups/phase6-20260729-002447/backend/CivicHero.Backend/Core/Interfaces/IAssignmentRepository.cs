using CivicHero.Backend.Core.Entities;

namespace CivicHero.Backend.Core.Interfaces;

public interface IAssignmentRepository : IRepository<ComplaintAssignment>
{
    IQueryable<ComplaintAssignment> QueryWithDetails(bool asTracking = false);
    Task<ComplaintAssignment?> GetCurrentByComplaintIdAsync(long complaintId, bool asTracking = false, CancellationToken cancellationToken = default);
}
