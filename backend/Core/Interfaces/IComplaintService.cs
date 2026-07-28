using CivicHero.Backend.Core.DTOs.Complaints;

namespace CivicHero.Backend.Core.Interfaces;

public interface IComplaintService
{
    Task<ComplaintDetailResponse> CreateAsync(
        ComplaintCreateRequest request);

    Task<ComplaintDetailResponse?> GetByIdAsync(int id);

    Task<List<ComplaintRowDto>> GetAllAsync(
        ComplaintQueryParameters query);

    Task<bool> UpdateAsync(
        int id,
        ComplaintUpdateRequest request);

    Task<bool> AssignOfficerAsync(
        int id,
        AssignOfficerRequest request);

    Task<bool> AssignContractorAsync(
        int id,
        AssignContractorRequest request);

    Task<bool> UpdateStatusAsync(
        int id,
        StatusUpdateRequest request);

    Task<bool> AddTimelineNoteAsync(
        int id,
        TimelineNoteCreateDto request);

    Task<List<TimelineRowDto>?> GetTimelineAsync(int id);

    Task<bool> ArchiveAsync(int id);

    Task<DashboardCounterDto> GetDashboardAsync();

    Task<List<ComplaintRowDto>> GetByWardAsync(int wardId);

    Task<List<ComplaintRowDto>> GetNearbyAsync(
        double latitude,
        double longitude,
        double radiusKm);
}