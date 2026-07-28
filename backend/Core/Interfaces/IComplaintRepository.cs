using CivicHero.Backend.Core.DTOs.Complaints;
using CivicHero.Backend.Core.Entities;

namespace CivicHero.Backend.Core.Interfaces;

public interface IComplaintRepository
{
    Task<Complaint?> GetByIdAsync(int id);

    Task<List<Complaint>> GetAllAsync(
        ComplaintQueryParameters query);

    Task<Complaint> CreateAsync(Complaint complaint);

    Task UpdateAsync(Complaint complaint);

    Task<List<Complaint>> GetByWardAsync(int wardId);

    Task<List<Complaint>> GetNearbyAsync(
        double latitude,
        double longitude,
        double radiusKm);

    Task<DashboardCounterDto> GetDashboardStatsAsync();

    Task AddTimelineAsync(ComplaintTimeline timeline);

    Task SaveChangesAsync();
}