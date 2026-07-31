using CivicHero.Backend.Core.Entities;

namespace CivicHero.Backend.Core.Interfaces;

public interface IDisputeAuditLogRepository : IRepository<DisputeAuditLog>
{
    Task<IReadOnlyList<DisputeAuditLog>> GetByComplaintIdAsync(long complaintId, CancellationToken cancellationToken = default);
}
