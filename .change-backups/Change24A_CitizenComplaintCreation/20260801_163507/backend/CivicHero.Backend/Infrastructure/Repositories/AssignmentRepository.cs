using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Repositories;

public sealed class AssignmentRepository : Repository<ComplaintAssignment>, IAssignmentRepository
{
    public AssignmentRepository(CivicDbContext dbContext) : base(dbContext) { }

    public IQueryable<ComplaintAssignment> QueryWithDetails(bool asTracking = false)
    {
        var query = asTracking ? DbSet.AsQueryable() : DbSet.AsNoTracking();
        return query
            .Include(entity => entity.Officer)
                .ThenInclude(entity => entity.Department)
            .Include(entity => entity.Officer)
                .ThenInclude(entity => entity.Ward)
            .Include(entity => entity.AssignedBy)
            .Include(entity => entity.Complaint)
                .ThenInclude(entity => entity.Citizen)
            .Include(entity => entity.Complaint)
                .ThenInclude(entity => entity.Department)
            .Include(entity => entity.Complaint)
                .ThenInclude(entity => entity.Ward)
            .Include(entity => entity.Complaint)
                .ThenInclude(entity => entity.ProgressUpdates)
                    .ThenInclude(entity => entity.Officer)
            .Include(entity => entity.Complaint)
                .ThenInclude(entity => entity.Images);
    }

    public Task<ComplaintAssignment?> GetCurrentByComplaintIdAsync(
        long complaintId,
        bool asTracking = false,
        CancellationToken cancellationToken = default) =>
        QueryWithDetails(asTracking)
            .FirstOrDefaultAsync(entity => entity.ComplaintId == complaintId && entity.IsCurrent, cancellationToken);
}
