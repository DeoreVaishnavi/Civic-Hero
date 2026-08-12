using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Repositories;

public sealed class ContractorRepository : Repository<Contractor>, IContractorRepository
{
    public ContractorRepository(CivicDbContext dbContext) : base(dbContext) { }

    public async Task<IReadOnlyList<Contractor>> GetByDepartmentIdAsync(
        long departmentId,
        CancellationToken cancellationToken = default) =>
        await Query()
            .Where(entity => entity.DepartmentId == departmentId)
            .OrderBy(entity => entity.CompanyName)
            .ToListAsync(cancellationToken);
}
