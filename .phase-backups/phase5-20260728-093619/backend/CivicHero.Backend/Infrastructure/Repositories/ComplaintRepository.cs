using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Repositories;

public sealed class ComplaintRepository : Repository<Complaint>, IComplaintRepository
{
    public ComplaintRepository(CivicDbContext dbContext) : base(dbContext) { }

    public async Task<IReadOnlyList<Complaint>> GetByCitizenIdAsync(
        long citizenId,
        CancellationToken cancellationToken = default) =>
        await Query()
            .Where(entity => entity.CitizenId == citizenId)
            .OrderByDescending(entity => entity.CreatedAt)
            .ToListAsync(cancellationToken);
}
