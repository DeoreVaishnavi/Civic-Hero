using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Infrastructure.Data;

namespace CivicHero.Backend.Infrastructure.Repositories;

public sealed class RedemptionRepository : Repository<Redemption>
{
    public RedemptionRepository(CivicDbContext dbContext) : base(dbContext) { }
}
