using CivicHero.Backend.Core.DTOs.Complaints;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Interfaces;

namespace CivicHero.Backend.Core.Services;

public class ComplaintService : IComplaintService
{
    private readonly IComplaintRepository _repository;

    public ComplaintService(IComplaintRepository repository)
    {
        _repository = repository;
    }

    public async Task<ComplaintDetailResponse> CreateAsync(
        ComplaintCreateRequest request)
    {
        var complaint = new Complaint
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            CitizenId = request.CitizenId,
            WardId = request.WardId,
            DepartmentId = request.DepartmentId,
            Priority = request.Priority,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Address = request.Address.Trim(),

            Status = ComplaintStatus.Submitted,

            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.CreateAsync(complaint);

        await _repository.AddTimelineAsync(
            new ComplaintTimeline
            {
                ComplaintId = complaint.Id,
                ActionType = "Complaint Created",
                Notes = "Complaint submitted by citizen.",
                PerformedBy = complaint.CitizenId,
                CreatedAt = DateTime.UtcNow
            });

        await _repository.SaveChangesAsync();

        return MapDetail(complaint);
    }

    public async Task<ComplaintDetailResponse?> GetByIdAsync(
        int id)
    {
        var complaint =
            await _repository.GetByIdAsync(id);

        return complaint == null
            ? null
            : MapDetail(complaint);
    }

    public async Task<List<ComplaintRowDto>> GetAllAsync(
        ComplaintQueryParameters query)
    {
        var complaints =
            await _repository.GetAllAsync(query);

        return complaints
            .Select(MapRow)
            .ToList();
    }

    public async Task<bool> UpdateAsync(
        int id,
        ComplaintUpdateRequest request)
    {
        var complaint =
            await _repository.GetByIdAsync(id);

        if (complaint == null)
            return false;

        complaint.Title = request.Title.Trim();
        complaint.Description = request.Description.Trim();
        complaint.WardId = request.WardId;
        complaint.DepartmentId = request.DepartmentId;
        complaint.Priority = request.Priority;
        complaint.Latitude = request.Latitude;
        complaint.Longitude = request.Longitude;
        complaint.Address = request.Address.Trim();
        complaint.ResolutionNotes = request.ResolutionNotes;

        complaint.UpdatedAt = DateTime.UtcNow;

        await _repository.AddTimelineAsync(
            new ComplaintTimeline
            {
                ComplaintId = complaint.Id,
                ActionType = "Complaint Updated",
                Notes = "Complaint details updated.",
                CreatedAt = DateTime.UtcNow
            });

        await _repository.UpdateAsync(complaint);

        return true;
    }

    public async Task<bool> AssignOfficerAsync(
        int id,
        AssignOfficerRequest request)
    {
        var complaint =
            await _repository.GetByIdAsync(id);

        if (complaint == null)
            return false;

        complaint.AssignedOfficerId =
            request.OfficerId;

        if (complaint.Status ==
            ComplaintStatus.UnderReview)
        {
            complaint.Status =
                ComplaintStatus.Assigned;
        }

        complaint.UpdatedAt = DateTime.UtcNow;

        await _repository.AddTimelineAsync(
            new ComplaintTimeline
            {
                ComplaintId = complaint.Id,
                ActionType = "Officer Assigned",
                Notes =
                    $"Officer {request.OfficerId} assigned.",
                CreatedAt = DateTime.UtcNow
            });

        await _repository.UpdateAsync(complaint);

        return true;
    }

    public async Task<bool> AssignContractorAsync(
        int id,
        AssignContractorRequest request)
    {
        var complaint =
            await _repository.GetByIdAsync(id);

        if (complaint == null)
            return false;

        complaint.AssignedContractorId =
            request.ContractorId;

        complaint.UpdatedAt = DateTime.UtcNow;

        await _repository.AddTimelineAsync(
            new ComplaintTimeline
            {
                ComplaintId = complaint.Id,
                ActionType = "Contractor Assigned",
                Notes =
                    $"Contractor {request.ContractorId} assigned.",
                CreatedAt = DateTime.UtcNow
            });

        await _repository.UpdateAsync(complaint);

        return true;
    }

    public async Task<bool> UpdateStatusAsync(
        int id,
        StatusUpdateRequest request)
    {
        var complaint =
            await _repository.GetByIdAsync(id);

        if (complaint == null)
            return false;

        if (!IsValidTransition(
            complaint.Status,
            request.Status))
        {
            throw new InvalidOperationException(
                $"Invalid status transition: " +
                $"{complaint.Status} -> {request.Status}");
        }

        var previousStatus = complaint.Status;

        complaint.Status = request.Status;
        complaint.UpdatedAt = DateTime.UtcNow;

        await _repository.AddTimelineAsync(
            new ComplaintTimeline
            {
                ComplaintId = complaint.Id,
                ActionType = "Status Updated",
                Notes =
                    request.Notes ??
                    $"{previousStatus} -> {request.Status}",
                CreatedAt = DateTime.UtcNow
            });

        await _repository.UpdateAsync(complaint);

        return true;
    }

    private static bool IsValidTransition(
        ComplaintStatus current,
        ComplaintStatus next)
    {
        return current switch
        {
            ComplaintStatus.Submitted =>
                next == ComplaintStatus.UnderReview ||
                next == ComplaintStatus.Rejected,

            ComplaintStatus.UnderReview =>
                next == ComplaintStatus.Assigned ||
                next == ComplaintStatus.Rejected,

            ComplaintStatus.Assigned =>
                next == ComplaintStatus.InProgress,

            ComplaintStatus.InProgress =>
                next == ComplaintStatus.Resolved,

            ComplaintStatus.Resolved =>
                next == ComplaintStatus.Closed,

            _ => false
        };
    }

    public async Task<bool> AddTimelineNoteAsync(
        int id,
        TimelineNoteCreateDto request)
    {
        var complaint =
            await _repository.GetByIdAsync(id);

        if (complaint == null)
            return false;

        await _repository.AddTimelineAsync(
            new ComplaintTimeline
            {
                ComplaintId = id,
                ActionType = "Timeline Note",
                Notes = request.Notes.Trim(),
                PerformedBy = request.PerformedBy,
                CreatedAt = DateTime.UtcNow
            });

        await _repository.SaveChangesAsync();

        return true;
    }

    public async Task<List<TimelineRowDto>?>
        GetTimelineAsync(int id)
    {
        var complaint =
            await _repository.GetByIdAsync(id);

        if (complaint == null)
            return null;

        return complaint.Timeline
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new TimelineRowDto
            {
                Id = x.Id,
                ActionType = x.ActionType,
                Notes = x.Notes,
                PerformedBy = x.PerformedBy,
                CreatedAt = x.CreatedAt
            })
            .ToList();
    }

    public async Task<bool> ArchiveAsync(int id)
    {
        var complaint =
            await _repository.GetByIdAsync(id);

        if (complaint == null)
            return false;

        complaint.Status =
            ComplaintStatus.Archived;

        complaint.UpdatedAt =
            DateTime.UtcNow;

        await _repository.AddTimelineAsync(
            new ComplaintTimeline
            {
                ComplaintId = id,
                ActionType = "Complaint Archived",
                Notes = "Complaint archived by administrator.",
                CreatedAt = DateTime.UtcNow
            });

        await _repository.UpdateAsync(complaint);

        return true;
    }

    public Task<DashboardCounterDto>
        GetDashboardAsync()
    {
        return _repository.GetDashboardStatsAsync();
    }

    public async Task<List<ComplaintRowDto>>
        GetByWardAsync(int wardId)
    {
        var complaints =
            await _repository.GetByWardAsync(wardId);

        return complaints.Select(MapRow).ToList();
    }

    public async Task<List<ComplaintRowDto>>
        GetNearbyAsync(
            double latitude,
            double longitude,
            double radiusKm)
    {
        var complaints =
            await _repository.GetNearbyAsync(
                latitude,
                longitude,
                radiusKm);

        return complaints.Select(MapRow).ToList();
    }

    private static ComplaintRowDto MapRow(
        Complaint x)
    {
        return new ComplaintRowDto
        {
            Id = x.Id,
            Title = x.Title,
            Status = x.Status,
            Priority = x.Priority,
            WardId = x.WardId,
            DepartmentId = x.DepartmentId,
            Address = x.Address,
            CreatedAt = x.CreatedAt
        };
    }

    private static ComplaintDetailResponse MapDetail(
        Complaint x)
    {
        return new ComplaintDetailResponse
        {
            Id = x.Id,
            Title = x.Title,
            Description = x.Description,
            CitizenId = x.CitizenId,
            WardId = x.WardId,
            DepartmentId = x.DepartmentId,
            AssignedOfficerId = x.AssignedOfficerId,
            AssignedContractorId =
                x.AssignedContractorId,
            Status = x.Status,
            Priority = x.Priority,
            Latitude = x.Latitude,
            Longitude = x.Longitude,
            Address = x.Address,
            ResolutionNotes = x.ResolutionNotes,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,

            Timeline = x.Timeline
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TimelineRowDto
                {
                    Id = t.Id,
                    ActionType = t.ActionType,
                    Notes = t.Notes,
                    PerformedBy = t.PerformedBy,
                    CreatedAt = t.CreatedAt
                })
                .ToList(),

            VoteCount = 0
        };
    }
}