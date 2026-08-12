using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Repositories;

public sealed class ComplaintRepository : Repository<Complaint>, IComplaintRepository
{
    public ComplaintRepository(CivicDbContext dbContext) : base(dbContext) { }

    public IQueryable<Complaint> QueryWithSummary(bool asTracking = false)
    {
        var query = asTracking ? DbSet.AsQueryable() : DbSet.AsNoTracking();
        return query
            .Include(entity => entity.Citizen)
            .Include(entity => entity.AssignedOfficer)
            .Include(entity => entity.Department)
            .Include(entity => entity.Ward)
            .Include(entity => entity.Images)
            .Include(entity => entity.Votes);
    }

    public IQueryable<Complaint> QueryWithDetails(bool asTracking = false) =>
        QueryWithSummary(asTracking)
            .Include(entity => entity.Timeline)
                .ThenInclude(item => item.User);

    public Task<Complaint?> GetDetailsAsync(
        long id,
        bool asTracking = false,
        CancellationToken cancellationToken = default) =>
        QueryWithDetails(asTracking).FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Complaint>> GetByCitizenIdAsync(
        long citizenId,
        CancellationToken cancellationToken = default) =>
        await QueryWithSummary()
            .Where(entity => entity.CitizenId == citizenId)
            .OrderByDescending(entity => entity.CreatedAt)
            .ToListAsync(cancellationToken);
}
