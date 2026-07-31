using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Infrastructure.Data;

namespace CivicHero.Backend.Infrastructure.Repositories;

public sealed class ReputationLogRepository : Repository<ReputationLog>
{
    public ReputationLogRepository(CivicDbContext dbContext) : base(dbContext) { }
}
