using CivicHero.Backend.Core.Entities;

namespace CivicHero.Backend.Core.Interfaces;

public interface IComplaintRepository : IRepository<Complaint>
{
    Task<IReadOnlyList<Complaint>> GetByCitizenIdAsync(long citizenId, CancellationToken cancellationToken = default);
}
