using CivicHero.Backend.Core.DTOs.Common;
using CivicHero.Backend.Core.DTOs.Complaints;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Core.Services;

public sealed class ComplaintService : IComplaintService
{
    private static readonly string[] Categories =
    [
        "Pothole", "Garbage", "Streetlight", "Water Leakage", "Drainage",
        "Road Damage", "Public Safety", "Illegal Dumping", "Other"
    ];

    private static readonly HashSet<ComplaintStatus> OpenStatuses =
    [
        ComplaintStatus.Created, ComplaintStatus.AiTriage, ComplaintStatus.FraudReview,
        ComplaintStatus.Assigned, ComplaintStatus.ReassignmentPending, ComplaintStatus.InProgress,
        ComplaintStatus.Escalated, ComplaintStatus.Resolved, ComplaintStatus.VerificationPending,
        ComplaintStatus.Disputed, ComplaintStatus.Appealed
    ];

    private readonly IComplaintRepository _complaints;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorageService _storage;
    private readonly ICurrentUserService _currentUser;
    private readonly IEmergencyReviewService _emergencyReviews;

    public ComplaintService(
        IComplaintRepository complaints,
        IUnitOfWork unitOfWork,
        IStorageService storage,
        ICurrentUserService currentUser,
        IEmergencyReviewService emergencyReviews)
    {
        _complaints = complaints;
        _unitOfWork = unitOfWork;
        _storage = storage;
        _currentUser = currentUser;
        _emergencyReviews = emergencyReviews;
    }

    public async Task<ComplaintDetailResponse> CreateAsync(CreateComplaintRequest request, CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId();
        EnsureCitizen();
        await ValidateScopeAsync(request.DepartmentId, request.WardId, cancellationToken);
        var category = NormalizeCategory(request.Category);
        var citizenSeverity = NormalizeCitizenSeverity(request.CitizenSeverity);
        var evidenceFiles = request.Evidence.Concat(request.Images).ToArray();
        var uploadedKeys = new List<string>();

        try
        {
            var complaint = new Complaint
            {
                CitizenId = userId,
                DepartmentId = request.DepartmentId,
                WardId = request.WardId,
                Title = request.Title.Trim(),
                Description = request.Description.Trim(),
                Category = category,
                Status = ComplaintStatus.Created,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Address = ComposeAddress(request.Address, request.Landmark),
                PossibleEmergency = request.PossibleEmergency,
                EmergencyReviewStatus = request.PossibleEmergency ? EmergencyReviewStatus.Pending : EmergencyReviewStatus.NotRequested,
                EmergencyRequestedAt = request.PossibleEmergency ? DateTimeOffset.UtcNow : null,
                Priority = request.PossibleEmergency ? ComplaintPriority.High : ComplaintPriority.Medium
            };

            if (request.PossibleEmergency)
            {
                complaint.EmergencyReviews.Add(_emergencyReviews.CreatePendingReview(complaint, request.EmergencyReason));
                complaint.Timeline.Add(new ComplaintTimeline
                {
                    UserId = userId,
                    EventType = "POSSIBLE_EMERGENCY_REPORTED",
                    Description = "Possible emergency flagged for official review. Critical priority has not been self-assigned.",
                    Timestamp = DateTimeOffset.UtcNow
                });
            }

            complaint.Timeline.Add(new ComplaintTimeline
            {
                UserId = userId,
                EventType = "CITIZEN_SEVERITY_REPORTED",
                Description = $"Citizen reported {citizenSeverity} severity. Official priority remains subject to CivicHero triage and authorized review.",
                Timestamp = DateTimeOffset.UtcNow
            });
            complaint.Timeline.Add(new ComplaintTimeline
            {
                UserId = userId,
                EventType = "CREATED",
                Description = "Complaint submitted by the citizen.",
                Timestamp = DateTimeOffset.UtcNow
            });

            foreach (var file in evidenceFiles)
            {
                var extension = await ValidateCitizenEvidenceAsync(file, cancellationToken);
                var objectKey = $"complaints/{userId}/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{extension}";
                await using var stream = file.OpenReadStream();
                await _storage.UploadAsync(
                    stream,
                    objectKey,
                    file.ContentType,
                    new Dictionary<string, string>
                    {
                        ["citizen-id"] = userId.ToString(),
                        ["original-file-name"] = Path.GetFileName(file.FileName),
                        ["evidence-type"] = EvidenceType(file.ContentType)
                    },
                    cancellationToken);

                uploadedKeys.Add(objectKey);
                complaint.Images.Add(new ComplaintImage
                {
                    S3Key = objectKey,
                    FileName = Path.GetFileName(file.FileName),
                    FileSize = file.Length,
                    MimeType = file.ContentType,
                    IsResolutionEvidence = false,
                    UploadedAt = DateTimeOffset.UtcNow
                });
            }

            await _complaints.AddAsync(complaint, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return await GetByIdAsync(complaint.Id, cancellationToken);
        }
        catch
        {
            foreach (var key in uploadedKeys)
            {
                try { await _storage.DeleteAsync(key, cancellationToken); }
                catch { /* Preserve the original exception; orphan cleanup can be retried operationally. */ }
            }
            throw;
        }
    }

    public async Task<ComplaintDetailResponse> RemoveEvidenceAsync(long complaintId, long evidenceId, CancellationToken cancellationToken = default)
    {
        EnsureCitizen();
        var userId = RequireUserId();
        var complaint = await _complaints.GetDetailsAsync(complaintId, true, cancellationToken)
            ?? throw new NotFoundException("Complaint was not found.");

        if (complaint.CitizenId != userId)
            throw new UnauthorizedAccessException("Only the citizen who created this complaint can remove its evidence.");
        if (!CanEditStatus(complaint.Status) || complaint.AssignedOfficerId is not null)
            throw new BusinessRuleViolationException("Submitted evidence can be removed only before the complaint is assigned or enters official review.");

        var evidence = complaint.Images.FirstOrDefault(item => item.Id == evidenceId)
            ?? throw new NotFoundException("Complaint evidence was not found.");
        if (evidence.IsResolutionEvidence || !evidence.S3Key.StartsWith("complaints/", StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleViolationException("This evidence is part of an official workflow and cannot be removed using the Citizen complaint form.");

        var objectKey = evidence.S3Key;
        var fileName = evidence.FileName;
        _unitOfWork.Repository<ComplaintImage>().Remove(evidence);
        complaint.Timeline.Add(new ComplaintTimeline
        {
            UserId = userId,
            EventType = "CITIZEN_EVIDENCE_REMOVED",
            Description = $"Citizen removed submitted evidence: {fileName}.",
            Timestamp = DateTimeOffset.UtcNow
        });
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try { await _storage.DeleteAsync(objectKey, cancellationToken); }
        catch { /* The database no longer exposes the file; orphan storage cleanup can be retried operationally. */ }

        return await GetByIdAsync(complaintId, cancellationToken);
    }

    public async Task<PagedResponse<ComplaintResponse>> GetAsync(ComplaintQuery query, CancellationToken cancellationToken = default)
    {
        var source = ApplyRoleScope(_complaints.QueryWithSummary(), includeCitizenWithdrawn: false);
        source = ApplyFilters(source, query);
        return await ToPagedResponseAsync(source, query, cancellationToken);
    }

    public async Task<PagedResponse<ComplaintResponse>> GetPublicAsync(ComplaintQuery query, CancellationToken cancellationToken = default)
    {
        var source = _complaints.QueryWithSummary()
            .Where(item => item.Status != ComplaintStatus.Withdrawn &&
                           item.Status != ComplaintStatus.ClosedFraud &&
                           item.Status != ComplaintStatus.Merged);
        source = ApplyFilters(source, query);
        return await ToPagedResponseAsync(source, query, cancellationToken);
    }

    public async Task<PagedResponse<ComplaintResponse>> GetMineAsync(ComplaintQuery query, CancellationToken cancellationToken = default)
    {
        EnsureCitizen();
        var userId = RequireUserId();
        var source = _complaints.QueryWithSummary().Where(entity => entity.CitizenId == userId);

        // A Citizen-deleted complaint is retained as Withdrawn for auditability, but it
        // should disappear from the normal "My complaints" feed. Citizens can still
        // review it explicitly by selecting the Withdrawn status filter.
        if (string.IsNullOrWhiteSpace(query.Status))
            source = source.Where(entity => entity.Status != ComplaintStatus.Withdrawn);

        source = ApplyFilters(source, query);
        return await ToPagedResponseAsync(source, query, cancellationToken);
    }

    public async Task<ComplaintDetailResponse> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var complaint = await _complaints.GetDetailsAsync(id, false, cancellationToken)
            ?? throw new NotFoundException("Complaint was not found.");
        EnsureCanView(complaint);
        return MapDetail(complaint);
    }

    public async Task<ComplaintDetailResponse> UpdateAsync(long id, UpdateComplaintRequest request, CancellationToken cancellationToken = default)
    {
        var complaint = await _complaints.GetDetailsAsync(id, true, cancellationToken)
            ?? throw new NotFoundException("Complaint was not found.");
        EnsureCanEdit(complaint);
        await ValidateScopeAsync(request.DepartmentId, request.WardId, cancellationToken);

        complaint.Title = request.Title.Trim();
        complaint.Description = request.Description.Trim();
        complaint.Category = NormalizeCategory(request.Category);
        complaint.DepartmentId = request.DepartmentId;
        complaint.WardId = request.WardId;
        complaint.Latitude = request.Latitude;
        complaint.Longitude = request.Longitude;
        complaint.Address = request.Address.Trim();
        complaint.Timeline.Add(new ComplaintTimeline
        {
            UserId = RequireUserId(),
            EventType = "UPDATED",
            Description = "Complaint details were updated.",
            Timestamp = DateTimeOffset.UtcNow
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ComplaintDetailResponse> WithdrawAsync(long id, CancellationToken cancellationToken = default)
    {
        EnsureCitizen();
        var complaint = await _complaints.GetDetailsAsync(id, true, cancellationToken)
            ?? throw new NotFoundException("Complaint was not found.");
        var userId = RequireUserId();

        if (complaint.CitizenId != userId)
            throw new UnauthorizedAccessException("Only the citizen who created this complaint can delete it.");
        if (!CanDeleteBeforeAssignment(complaint))
            throw new BusinessRuleViolationException("A complaint can be deleted only before it is assigned to an officer.");

        // Preserve the complaint and timeline for audit/legal traceability instead of
        // physically removing related evidence, votes and workflow records.
        complaint.Status = ComplaintStatus.Withdrawn;
        complaint.Timeline.Add(new ComplaintTimeline
        {
            UserId = userId,
            EventType = "DELETED_BY_CITIZEN",
            Description = "Complaint deleted by the citizen before officer assignment.",
            Timestamp = DateTimeOffset.UtcNow
        });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<int> UpvoteAsync(long id, CancellationToken cancellationToken = default)
    {
        EnsureCitizen();
        var userId = RequireUserId();
        var complaint = await _complaints.GetDetailsAsync(id, true, cancellationToken)
            ?? throw new NotFoundException("Complaint was not found.");
        if (complaint.Status is ComplaintStatus.Withdrawn or ComplaintStatus.ClosedFraud or ComplaintStatus.Merged)
            throw new BusinessRuleViolationException("This complaint cannot receive upvotes.");
        if (complaint.Votes.Any(vote => vote.UserId == userId))
            return complaint.Votes.Count;

        complaint.Votes.Add(new ComplaintVote
        {
            UserId = userId,
            VotedAt = DateTimeOffset.UtcNow
        });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return complaint.Votes.Count;
    }

    public async Task<int> RemoveUpvoteAsync(long id, CancellationToken cancellationToken = default)
    {
        EnsureCitizen();
        var userId = RequireUserId();
        var complaint = await _complaints.GetDetailsAsync(id, true, cancellationToken)
            ?? throw new NotFoundException("Complaint was not found.");
        var vote = complaint.Votes.FirstOrDefault(item => item.UserId == userId);
        if (vote is null) return complaint.Votes.Count;

        _unitOfWork.Repository<ComplaintVote>().Remove(vote);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Math.Max(0, complaint.Votes.Count - 1);
    }

    public async Task<IReadOnlyList<ComplaintResponse>> GetNearbyAsync(NearbyComplaintQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Latitude is < -90 or > 90 || query.Longitude is < -180 or > 180)
            throw new ValidationException(["A valid latitude and longitude are required."]);

        var radius = Math.Clamp(query.RadiusKm, 0.25, 25);
        var limit = Math.Clamp(query.Limit, 1, 50);
        var latitudeDelta = radius / 111d;
        var longitudeFactor = Math.Max(0.2d, Math.Cos((double)query.Latitude * Math.PI / 180d));
        var longitudeDelta = radius / (111d * longitudeFactor);
        var minLatitude = (decimal)((double)query.Latitude - latitudeDelta);
        var maxLatitude = (decimal)((double)query.Latitude + latitudeDelta);
        var minLongitude = (decimal)((double)query.Longitude - longitudeDelta);
        var maxLongitude = (decimal)((double)query.Longitude + longitudeDelta);

        var candidates = await _complaints.QueryWithSummary()
            .Where(item => item.Status != ComplaintStatus.Withdrawn &&
                           item.Status != ComplaintStatus.ClosedFraud &&
                           item.Status != ComplaintStatus.Merged &&
                           item.Latitude >= minLatitude && item.Latitude <= maxLatitude &&
                           item.Longitude >= minLongitude && item.Longitude <= maxLongitude)
            .OrderByDescending(item => item.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        return candidates
            .Select(item => new { Item = item, Distance = HaversineKm(query.Latitude, query.Longitude, item.Latitude, item.Longitude) })
            .Where(item => item.Distance <= radius)
            .OrderBy(item => item.Distance)
            .Take(limit)
            .Select(item => MapSummary(item.Item, item.Distance, publicView: true))
            .ToArray();
    }

    public async Task<ComplaintMetadataResponse> GetMetadataAsync(CancellationToken cancellationToken = default)
    {
        var departments = await _unitOfWork.Repository<Department>().Query()
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name)
            .Select(item => new DepartmentOptionDto { Id = item.Id, Name = item.Name, Code = item.Code })
            .ToListAsync(cancellationToken);
        var wards = await _unitOfWork.Repository<Ward>().Query()
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name)
            .Select(item => new WardOptionDto { Id = item.Id, DepartmentId = item.DepartmentId, Name = item.Name, Code = item.Code })
            .ToListAsync(cancellationToken);

        return new ComplaintMetadataResponse
        {
            Categories = Categories,
            Departments = departments,
            Wards = wards
        };
    }

    public async Task<ComplaintDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var source = ApplyDashboardScope(_complaints.QueryWithSummary());
        var all = await source.OrderByDescending(item => item.CreatedAt).ToListAsync(cancellationToken);
        return new ComplaintDashboardResponse
        {
            Total = all.Count,
            Open = all.Count(item => OpenStatuses.Contains(item.Status)),
            InProgress = all.Count(item => item.Status == ComplaintStatus.InProgress),
            Resolved = all.Count(item => item.Status is ComplaintStatus.Resolved or ComplaintStatus.VerificationPending),
            Closed = all.Count(item => item.Status is ComplaintStatus.Closed or ComplaintStatus.ClosedAuto),
            Withdrawn = all.Count(item => item.Status == ComplaintStatus.Withdrawn),
            TotalUpvotes = all.Sum(item => item.Votes.Count),
            RecentComplaints = all.Take(5).Select(item => MapSummary(item)).ToArray()
        };
    }

    public async Task<StorageDownload> DownloadImageAsync(long complaintId, long imageId, CancellationToken cancellationToken = default)
    {
        var complaint = await _complaints.GetDetailsAsync(complaintId, false, cancellationToken)
            ?? throw new NotFoundException("Complaint was not found.");
        EnsureCanView(complaint);
        var image = complaint.Images.FirstOrDefault(item => item.Id == imageId)
            ?? throw new NotFoundException("Complaint image was not found.");
        return await _storage.DownloadAsync(image.S3Key, image.FileName, cancellationToken)
            ?? throw new NotFoundException("The image file is no longer available in storage.");
    }

    private IQueryable<Complaint> ApplyRoleScope(IQueryable<Complaint> query, bool includeCitizenWithdrawn)
    {
        var role = _currentUser.Role ?? string.Empty;
        if (RoleIs("Admin") || RoleIs("SuperAdmin")) return query;
        if (RoleIs("Supervisor"))
        {
            var departmentId = _currentUser.DepartmentId ?? -1;
            return query.Where(item => item.DepartmentId == departmentId);
        }
        if (RoleIs("Officer"))
        {
            var departmentId = _currentUser.DepartmentId ?? -1;
            var wardId = _currentUser.WardId ?? -1;
            return query.Where(item => item.DepartmentId == departmentId && item.WardId == wardId);
        }
        if (string.Equals(role, "Citizen", StringComparison.OrdinalIgnoreCase))
        {
            var userId = RequireUserId();
            return includeCitizenWithdrawn
                ? query.Where(item => item.CitizenId == userId || item.Status != ComplaintStatus.Withdrawn)
                : query.Where(item => item.Status != ComplaintStatus.Withdrawn);
        }
        return query.Where(_ => false);
    }

    private IQueryable<Complaint> ApplyDashboardScope(IQueryable<Complaint> query)
    {
        if (RoleIs("Citizen"))
        {
            var userId = RequireUserId();
            return query.Where(item => item.CitizenId == userId);
        }
        return ApplyRoleScope(query, includeCitizenWithdrawn: true);
    }

    private static IQueryable<Complaint> ApplyFilters(IQueryable<Complaint> source, ComplaintQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<ComplaintStatus>(query.Status, true, out var status))
            source = source.Where(item => item.Status == status);
        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var category = query.Category.Trim();
            source = source.Where(item => item.Category == category);
        }
        if (query.DepartmentId.HasValue) source = source.Where(item => item.DepartmentId == query.DepartmentId.Value);
        if (query.WardId.HasValue) source = source.Where(item => item.WardId == query.WardId.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = $"%{query.Search.Trim()}%";
            source = source.Where(item => EF.Functions.Like(item.Title, search) ||
                                          EF.Functions.Like(item.Description, search) ||
                                          EF.Functions.Like(item.Address, search) ||
                                          EF.Functions.Like(item.Category, search) ||
                                          EF.Functions.Like(item.Department.Name, search) ||
                                          EF.Functions.Like(item.Ward.Name, search));
        }
        return source;
    }

    private async Task<PagedResponse<ComplaintResponse>> ToPagedResponseAsync(
        IQueryable<Complaint> source,
        ComplaintQuery query,
        CancellationToken cancellationToken)
    {
        var total = await source.CountAsync(cancellationToken);
        source = (query.SortBy ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "oldest" => source.OrderBy(item => item.CreatedAt),
            "most-supported" or "supports" => source.OrderByDescending(item => item.Votes.Count).ThenByDescending(item => item.CreatedAt),
            "recently-updated" or "updated" => source.OrderByDescending(item => item.UpdatedAt).ThenByDescending(item => item.CreatedAt),
            _ when string.Equals(query.SortOrder, "asc", StringComparison.OrdinalIgnoreCase) => source.OrderBy(item => item.CreatedAt),
            _ => source.OrderByDescending(item => item.CreatedAt)
        };
        var items = await source.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        return new PagedResponse<ComplaintResponse>
        {
            Items = items.Select(item => MapSummary(item)).ToArray(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }

    private ComplaintDetailResponse MapDetail(Complaint entity) => new()
    {
        Complaint = MapSummary(entity),
        CitizenName = entity.IsAnonymous ? "Anonymous citizen" : entity.Citizen.FullName,
        AssignedOfficerName = entity.AssignedOfficer?.FullName,
        ResolvedAt = entity.ResolvedAt,
        ClosedAt = entity.ClosedAt,
        Images = entity.Images.OrderBy(item => item.UploadedAt).Select(item => new ComplaintImageDto
        {
            Id = item.Id,
            FileName = item.FileName,
            FileSize = item.FileSize,
            MimeType = item.MimeType,
            IsResolutionEvidence = item.IsResolutionEvidence,
            UploadedAt = item.UploadedAt,
            DownloadPath = $"/complaints/{entity.Id}/images/{item.Id}",
            PublicUrl = item.S3Url
        }).ToArray(),
        Timeline = entity.Timeline.OrderByDescending(item => item.Timestamp).Select(item => new ComplaintTimelineResponse
        {
            Id = item.Id,
            EventType = item.EventType,
            Description = item.Description,
            ActorName = item.User?.FullName ?? "System",
            Timestamp = item.Timestamp
        }).ToArray()
    };

    private ComplaintResponse MapSummary(Complaint entity, double? distanceKm = null, bool publicView = false)
    {
        var userId = _currentUser.UserId;
        var isOwner = userId.HasValue && entity.CitizenId == userId.Value;
        var canAdminEdit = RoleIs("Admin") || RoleIs("SuperAdmin");
        return new ComplaintResponse
        {
            Id = entity.Id,
            ReferenceNumber = $"CH-{entity.CreatedAt:yyyy}-{entity.Id:D6}",
            Title = entity.Title,
            Description = entity.Description,
            Category = entity.Category,
            Status = entity.Status.ToString(),
            Priority = entity.Priority.ToString(),
            DepartmentId = entity.DepartmentId,
            DepartmentName = entity.Department.Name,
            WardId = entity.WardId,
            WardName = entity.Ward.Name,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            Address = entity.Address,
            ImageCount = entity.Images.Count,
            UpvoteCount = entity.Votes.Count,
            HasUpvoted = !publicView && userId.HasValue && entity.Votes.Any(vote => vote.UserId == userId.Value),
            IsOwner = !publicView && isOwner,
            CanEdit = !publicView && (canAdminEdit || (isOwner && CanEditStatus(entity.Status))),
            CanWithdraw = !publicView && isOwner && !entity.IsAnonymous && CanDeleteBeforeAssignment(entity),
            IsAnonymous = entity.IsAnonymous,
            PossibleEmergency = entity.PossibleEmergency,
            EmergencyReviewStatus = entity.EmergencyReviewStatus.ToString(),
            DistanceKm = distanceKm.HasValue ? Math.Round(distanceKm.Value, 2) : null,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private void EnsureCanView(Complaint complaint)
    {
        if (RoleIs("Admin") || RoleIs("SuperAdmin")) return;
        if (RoleIs("Citizen"))
        {
            if (complaint.CitizenId == RequireUserId() || complaint.Status != ComplaintStatus.Withdrawn) return;
            throw new UnauthorizedAccessException("You do not have access to this complaint.");
        }
        if (RoleIs("Supervisor") && complaint.DepartmentId == _currentUser.DepartmentId) return;
        if (RoleIs("Officer") && complaint.DepartmentId == _currentUser.DepartmentId && complaint.WardId == _currentUser.WardId) return;
        throw new UnauthorizedAccessException("This complaint is outside your assigned scope.");
    }

    private void EnsureCanEdit(Complaint complaint)
    {
        if (RoleIs("Admin") || RoleIs("SuperAdmin")) return;
        if (RoleIs("Citizen") && complaint.CitizenId == RequireUserId() && CanEditStatus(complaint.Status)) return;
        throw new UnauthorizedAccessException("This complaint cannot be edited by the current user.");
    }

    private async Task ValidateScopeAsync(long departmentId, long wardId, CancellationToken cancellationToken)
    {
        var departmentExists = await _unitOfWork.Repository<Department>().Query()
            .AnyAsync(item => item.Id == departmentId && item.IsActive, cancellationToken);
        if (!departmentExists) throw new ValidationException(["Select an active department."]);

        var wardExists = await _unitOfWork.Repository<Ward>().Query()
            .AnyAsync(item => item.Id == wardId && item.DepartmentId == departmentId && item.IsActive, cancellationToken);
        if (!wardExists) throw new ValidationException(["Select an active ward belonging to the chosen department."]);
    }

    private static string NormalizeCategory(string value)
    {
        var category = Categories.FirstOrDefault(item => string.Equals(item, value.Trim(), StringComparison.OrdinalIgnoreCase));
        return category ?? throw new ValidationException(["Select a supported complaint category."]);
    }

    private static string ComposeAddress(string address, string? landmark)
    {
        var normalizedAddress = address.Trim();
        var normalizedLandmark = landmark?.Trim();
        return string.IsNullOrWhiteSpace(normalizedLandmark)
            ? normalizedAddress
            : $"{normalizedAddress} | Landmark: {normalizedLandmark}";
    }

    private static string NormalizeCitizenSeverity(string value)
    {
        var severity = (value ?? string.Empty).Trim();
        return severity.ToLowerInvariant() switch
        {
            "low" => "Low",
            "medium" => "Medium",
            "high" => "High",
            "critical" => "Critical",
            _ => throw new ValidationException(["Select Low, Medium, High, or Critical as the reported severity."])
        };
    }

    private static string EvidenceType(string? mimeType) => mimeType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true
        ? "citizen-image"
        : mimeType?.StartsWith("video/", StringComparison.OrdinalIgnoreCase) == true ? "citizen-video" : "citizen-document";

    private static async Task<string> ValidateCitizenEvidenceAsync(Microsoft.AspNetCore.Http.IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length <= 0 || file.Length > 15L * 1024 * 1024)
            throw new ValidationException(["Each complaint evidence file must be between 1 byte and 15 MB."]);

        var mime = (file.ContentType ?? string.Empty).Trim().ToLowerInvariant();
        var allowed = mime switch
        {
            "image/jpeg" => new[] { ".jpg", ".jpeg" },
            "image/png" => new[] { ".png" },
            "image/webp" => new[] { ".webp" },
            "video/mp4" => new[] { ".mp4" },
            "video/webm" => new[] { ".webm" },
            "video/quicktime" => new[] { ".mov" },
            "application/pdf" => new[] { ".pdf" },
            "application/msword" => new[] { ".doc" },
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => new[] { ".docx" },
            _ => Array.Empty<string>()
        };
        if (allowed.Length == 0)
            throw new ValidationException(["Complaint evidence must be JPEG, PNG, WebP, MP4, WebM, MOV, PDF, DOC, or DOCX."]);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(extension, StringComparer.OrdinalIgnoreCase))
            throw new ValidationException([$"The file extension does not match the declared {mime} content type."]);

        await using var stream = file.OpenReadStream();
        var header = new byte[16];
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
        var valid = mime switch
        {
            "image/jpeg" => read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            "image/png" => read >= 8 && header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            "image/webp" => read >= 12 && System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WEBP",
            "video/mp4" or "video/quicktime" => read >= 8 && System.Text.Encoding.ASCII.GetString(header, 4, 4) == "ftyp",
            "video/webm" => read >= 4 && header[0] == 0x1A && header[1] == 0x45 && header[2] == 0xDF && header[3] == 0xA3,
            "application/pdf" => read >= 4 && System.Text.Encoding.ASCII.GetString(header, 0, 4) == "%PDF",
            "application/msword" => read >= 8 && header[..8].SequenceEqual(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }),
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => read >= 4 && header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04,
            _ => false
        };
        if (!valid) throw new ValidationException(["The complaint evidence file signature is invalid."]);
        return extension == ".jpeg" ? ".jpg" : extension;
    }

    private long RequireUserId() => _currentUser.UserId
        ?? throw new UnauthorizedAccessException("Authenticated user identifier is missing.");

    private void EnsureCitizen()
    {
        if (!RoleIs("Citizen")) throw new UnauthorizedAccessException("A Citizen account is required for this action.");
    }

    private bool RoleIs(string role) => string.Equals(_currentUser.Role, role, StringComparison.OrdinalIgnoreCase);
    private static bool CanEditStatus(ComplaintStatus status) => status is ComplaintStatus.Created or ComplaintStatus.AiTriage;
    private static bool CanDeleteBeforeAssignment(Complaint complaint) =>
        complaint.AssignedOfficerId is null &&
        complaint.Status is ComplaintStatus.Created or ComplaintStatus.AiTriage or ComplaintStatus.FraudReview;

    private static double HaversineKm(decimal latitude1, decimal longitude1, decimal latitude2, decimal longitude2)
    {
        const double earthRadiusKm = 6371.0088;
        var lat1 = DegreesToRadians((double)latitude1);
        var lat2 = DegreesToRadians((double)latitude2);
        var deltaLat = DegreesToRadians((double)(latitude2 - latitude1));
        var deltaLon = DegreesToRadians((double)(longitude2 - longitude1));
        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
        return earthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;
}

