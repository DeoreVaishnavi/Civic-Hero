using CivicHero.Backend.Core.Entities;

namespace CivicHero.Backend.Core.Interfaces;

public interface IComplaintRepository : IRepository<Complaint>
{
    IQueryable<Complaint> QueryWithSummary(bool asTracking = false);
    IQueryable<Complaint> QueryWithDetails(bool asTracking = false);
    Task<Complaint?> GetDetailsAsync(long id, bool asTracking = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Complaint>> GetByCitizenIdAsync(long citizenId, CancellationToken cancellationToken = default);
}
