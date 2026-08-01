using CivicHero.Backend.Core.DTOs.Complaints;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Core.Services;

public sealed class ComplaintDraftService : IComplaintDraftService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorageService _storage;
    private readonly ICurrentUserService _currentUser;

    public ComplaintDraftService(
        IUnitOfWork unitOfWork,
        IStorageService storage,
        ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _storage = storage;
        _currentUser = currentUser;
    }

    public async Task<ComplaintDraftResponse?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        EnsureCitizen();
        var draft = await QueryDrafts(false)
            .SingleOrDefaultAsync(entity => entity.CitizenId == RequireUserId(), cancellationToken);
        return draft is null ? null : Map(draft);
    }

    public async Task<ComplaintDraftResponse> SaveCurrentAsync(
        UpsertComplaintDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCitizen();
        var userId = RequireUserId();
        await ValidateOptionalScopeAsync(request.DepartmentId, request.WardId, cancellationToken);

        var draft = await QueryDrafts(true)
            .SingleOrDefaultAsync(entity => entity.CitizenId == userId, cancellationToken);
        if (draft is null)
        {
            draft = new ComplaintDraft { CitizenId = userId };
            await _unitOfWork.Repository<ComplaintDraft>().AddAsync(draft, cancellationToken);
        }

        draft.Title = NormalizeOptional(request.Title);
        draft.Description = NormalizeOptional(request.Description);
        draft.Category = NormalizeOptional(request.Category);
        draft.CitizenSeverity = NormalizeSeverity(request.CitizenSeverity);
        draft.DepartmentId = request.DepartmentId;
        draft.WardId = request.WardId;
        draft.Latitude = request.Latitude;
        draft.Longitude = request.Longitude;
        draft.Address = NormalizeOptional(request.Address);
        draft.Landmark = NormalizeOptional(request.Landmark);
        draft.PossibleEmergency = request.PossibleEmergency;
        draft.EmergencyReason = request.PossibleEmergency ? NormalizeOptional(request.EmergencyReason) : null;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(draft);
    }

    public async Task<ComplaintDraftEvidenceResponse> AddEvidenceAsync(
        Microsoft.AspNetCore.Http.IFormFile evidence,
        CancellationToken cancellationToken = default)
    {
        EnsureCitizen();
        var userId = RequireUserId();
        var extension = await ValidateEvidenceAsync(evidence, cancellationToken);
        var draft = await QueryDrafts(true)
            .SingleOrDefaultAsync(entity => entity.CitizenId == userId, cancellationToken);

        if (draft is null)
        {
            draft = new ComplaintDraft { CitizenId = userId };
            await _unitOfWork.Repository<ComplaintDraft>().AddAsync(draft, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        if (draft.Evidence.Count >= 8)
            throw new ValidationException(["A complaint draft can contain a maximum of eight evidence files."]);
        if (draft.Evidence.Sum(item => item.FileSize) + evidence.Length > 25L * 1024 * 1024)
            throw new ValidationException(["The total draft evidence size cannot exceed 25 MB."]);

        var objectKey = $"complaint-drafts/{userId}/{draft.Id}/{Guid.NewGuid():N}{extension}";
        try
        {
            await using var stream = evidence.OpenReadStream();
            await _storage.UploadAsync(
                stream,
                objectKey,
                evidence.ContentType,
                new Dictionary<string, string>
                {
                    ["citizen-id"] = userId.ToString(),
                    ["draft-id"] = draft.Id.ToString(),
                    ["original-file-name"] = Path.GetFileName(evidence.FileName),
                    ["evidence-type"] = EvidenceType(evidence.ContentType)
                },
                cancellationToken);

            var row = new ComplaintDraftEvidence
            {
                ComplaintDraftId = draft.Id,
                S3Key = objectKey,
                FileName = Path.GetFileName(evidence.FileName),
                FileSize = evidence.Length,
                MimeType = evidence.ContentType,
                UploadedAt = DateTimeOffset.UtcNow
            };
            draft.Evidence.Add(row);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Map(row);
        }
        catch
        {
            try { await _storage.DeleteAsync(objectKey, cancellationToken); }
            catch { }
            throw;
        }
    }

    public async Task RemoveEvidenceAsync(long evidenceId, CancellationToken cancellationToken = default)
    {
        EnsureCitizen();
        var draft = await QueryDrafts(true)
            .SingleOrDefaultAsync(entity => entity.CitizenId == RequireUserId(), cancellationToken)
            ?? throw new NotFoundException("Complaint draft was not found.");
        var evidence = draft.Evidence.SingleOrDefault(item => item.Id == evidenceId)
            ?? throw new NotFoundException("Draft evidence was not found.");

        var key = evidence.S3Key;
        _unitOfWork.Repository<ComplaintDraftEvidence>().Remove(evidence);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        try { await _storage.DeleteAsync(key, cancellationToken); }
        catch { }
    }

    public async Task<StorageDownload> DownloadEvidenceAsync(long evidenceId, CancellationToken cancellationToken = default)
    {
        EnsureCitizen();
        var draft = await QueryDrafts(false)
            .SingleOrDefaultAsync(entity => entity.CitizenId == RequireUserId(), cancellationToken)
            ?? throw new NotFoundException("Complaint draft was not found.");
        var evidence = draft.Evidence.SingleOrDefault(item => item.Id == evidenceId)
            ?? throw new NotFoundException("Draft evidence was not found.");
        return await _storage.DownloadAsync(evidence.S3Key, evidence.FileName, cancellationToken)
            ?? throw new NotFoundException("Draft evidence is unavailable in storage.");
    }

    public async Task DeleteCurrentAsync(CancellationToken cancellationToken = default)
    {
        EnsureCitizen();
        var draft = await QueryDrafts(true)
            .SingleOrDefaultAsync(entity => entity.CitizenId == RequireUserId(), cancellationToken);
        if (draft is null) return;

        var keys = draft.Evidence.Select(item => item.S3Key).ToArray();
        _unitOfWork.Repository<ComplaintDraft>().Remove(draft);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        foreach (var key in keys)
        {
            try { await _storage.DeleteAsync(key, cancellationToken); }
            catch { }
        }
    }

    private IQueryable<ComplaintDraft> QueryDrafts(bool tracking) =>
        _unitOfWork.Repository<ComplaintDraft>().Query(tracking).Include(entity => entity.Evidence);

    private async Task ValidateOptionalScopeAsync(long? departmentId, long? wardId, CancellationToken cancellationToken)
    {
        if (departmentId.HasValue)
        {
            var validDepartment = await _unitOfWork.Repository<Department>().Query()
                .AnyAsync(entity => entity.Id == departmentId.Value && entity.IsActive, cancellationToken);
            if (!validDepartment) throw new ValidationException(["Select an active department."]);
        }

        if (wardId.HasValue)
        {
            if (!departmentId.HasValue)
                throw new ValidationException(["Select a department before selecting a ward."]);
            var validWard = await _unitOfWork.Repository<Ward>().Query()
                .AnyAsync(entity => entity.Id == wardId.Value && entity.DepartmentId == departmentId.Value && entity.IsActive, cancellationToken);
            if (!validWard) throw new ValidationException(["Select an active ward belonging to the chosen department."]);
        }
    }

    private static ComplaintDraftResponse Map(ComplaintDraft draft) => new(
        draft.Id,
        draft.Title,
        draft.Description,
        draft.Category,
        draft.CitizenSeverity,
        draft.DepartmentId,
        draft.WardId,
        draft.Latitude,
        draft.Longitude,
        draft.Address,
        draft.Landmark,
        draft.PossibleEmergency,
        draft.EmergencyReason,
        draft.UpdatedAt,
        draft.Evidence.OrderBy(item => item.UploadedAt).Select(Map).ToArray());

    private static ComplaintDraftEvidenceResponse Map(ComplaintDraftEvidence evidence) => new(
        evidence.Id,
        evidence.FileName,
        evidence.FileSize,
        evidence.MimeType,
        evidence.UploadedAt);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeSeverity(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "low" => "Low",
        "high" => "High",
        "critical" => "Critical",
        _ => "Medium"
    };

    private static string EvidenceType(string? mimeType) => mimeType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true
        ? "citizen-image"
        : mimeType?.StartsWith("video/", StringComparison.OrdinalIgnoreCase) == true ? "citizen-video" : "citizen-document";

    private static async Task<string> ValidateEvidenceAsync(Microsoft.AspNetCore.Http.IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length <= 0 || file.Length > 15L * 1024 * 1024)
            throw new ValidationException(["Each draft evidence file must be between 1 byte and 15 MB."]);

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
            throw new ValidationException(["Draft evidence must be JPEG, PNG, WebP, MP4, WebM, MOV, PDF, DOC, or DOCX."]);

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
        if (!valid) throw new ValidationException(["The draft evidence file signature is invalid."]);
        return extension == ".jpeg" ? ".jpg" : extension;
    }

    private long RequireUserId() => _currentUser.UserId
        ?? throw new UnauthorizedAccessException("Authenticated user identifier is missing.");

    private void EnsureCitizen()
    {
        if (!string.Equals(_currentUser.Role, "Citizen", StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("A Citizen account is required for this action.");
    }
}
