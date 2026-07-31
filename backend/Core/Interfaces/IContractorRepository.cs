using CivicHero.Backend.Core.Entities;

namespace CivicHero.Backend.Core.Interfaces;

public interface IContractorRepository : IRepository<Contractor>
{
    Task<IReadOnlyList<Contractor>> GetByDepartmentIdAsync(long departmentId, CancellationToken cancellationToken = default);
}
