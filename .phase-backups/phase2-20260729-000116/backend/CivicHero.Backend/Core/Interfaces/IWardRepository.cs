using CivicHero.Backend.Core.Entities;

namespace CivicHero.Backend.Core.Interfaces;

public interface IWardRepository : IRepository<Ward>
{
    Task<Ward?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}
