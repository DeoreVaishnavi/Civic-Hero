using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Repositories;

public sealed class DisputeAuditLogRepository : Repository<DisputeAuditLog>, IDisputeAuditLogRepository
{
    public DisputeAuditLogRepository(CivicDbContext dbContext) : base(dbContext) { }

    public async Task<IReadOnlyList<DisputeAuditLog>> GetByComplaintIdAsync(
        long complaintId,
        CancellationToken cancellationToken = default) =>
        await Query()
            .Where(entity => entity.ComplaintId == complaintId)
            .OrderByDescending(entity => entity.RaisedAt)
            .ToListAsync(cancellationToken);
}
