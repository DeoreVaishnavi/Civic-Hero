using CivicHero.Backend.Core.Entities;

namespace CivicHero.Backend.Core.Interfaces;

public interface IDepartmentRepository : IRepository<Department>
{
    Task<Department?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}
