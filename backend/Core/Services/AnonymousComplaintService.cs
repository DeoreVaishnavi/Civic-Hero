using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using CivicHero.Backend.Core.DTOs.Anonymous;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Configurations;
using CivicHero.Backend.Infrastructure.Security;
using CivicHero.Backend.Infrastructure.Storage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Core.Services;

public sealed class AnonymousComplaintService : IAnonymousComplaintService
{
    private static readonly SemaphoreSlim ReporterLock = new(1, 1);
    private static readonly string[] Categories =
    [
        "Pothole", "Garbage", "Streetlight", "Water Leakage", "Drainage",
        "Road Damage", "Public Safety", "Illegal Dumping", "Other"
    ];

    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IStorageService _storage;
    private readonly ICaptchaVerifier _captchaVerifier;
    private readonly IEmergencyReviewService _emergencyReviews;
    private readonly AnonymousReportingOptions _options;
    private readonly IDataProtector _protector;

    public AnonymousComplaintService(
        IUnitOfWork unitOfWork,
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IStorageService storage,
        ICaptchaVerifier captchaVerifier,
        IEmergencyReviewService emergencyReviews,
        IOptions<AnonymousReportingOptions> options,
        IDataProtectionProvider dataProtectionProvider)
    {
        _unitOfWork = unitOfWork;
        _users = users;
        _passwordHasher = passwordHasher;
        _storage = storage;
        _captchaVerifier = captchaVerifier;
        _emergencyReviews = emergencyReviews;
        _options = options.Value;
        _protector = dataProtectionProvider.CreateProtector("CivicHero.AnonymousComplaintContact.v1");
    }

    public async Task<AnonymousComplaintCreatedResponse> CreateAsync(CreateAnonymousComplaintRequest request, string? remoteIp, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled) throw new BusinessRuleViolationException("Anonymous complaint reporting is currently disabled.");
        ValidateRequest(request);
        var validatedImages = request.Images.Select(file => new
        {
            File = file,
            Extension = ValidateImage(file.FileName, file.ContentType, file.Length)
        }).ToArray();
        var captcha = await _captchaVerifier.VerifyAsync(request.CaptchaToken, remoteIp, cancellationToken);
        if (!captcha.Success) throw new BusinessRuleViolationException(captcha.Error ?? "CAPTCHA verification failed.");

        var ipHash = string.IsNullOrWhiteSpace(remoteIp) ? null : Hash(remoteIp);
        if (ipHash is not null)
        {
            var since = DateTimeOffset.UtcNow.AddHours(-1);
            var count = await _unitOfWork.Repository<AnonymousComplaintAccess>().Query()
                .CountAsync(entity => entity.SubmissionIpHash == ipHash && entity.CreatedAt >= since, cancellationToken);
            if (count >= Math.Clamp(_options.MaximumSubmissionsPerHour, 1, 10))
                throw new BusinessRuleViolationException("Anonymous submission limit reached. Please try again later or sign in.");
        }

        await ValidateScopeAsync(request.DepartmentId, request.WardId, cancellationToken);
        var reporter = await GetOrCreateSystemReporterAsync(cancellationToken);
        var trackingToken = GenerateToken();
        var now = DateTimeOffset.UtcNow;
        var complaint = new Complaint
        {
            CitizenId = reporter.Id,
            DepartmentId = request.DepartmentId,
            WardId = request.WardId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Category = NormalizeCategory(request.Category),
            Status = ComplaintStatus.Created,
            Priority = request.PossibleEmergency ? ComplaintPriority.High : ComplaintPriority.Medium,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Address = request.Address.Trim(),
            IsAnonymous = true,
            PossibleEmergency = request.PossibleEmergency,
            EmergencyReviewStatus = request.PossibleEmergency ? EmergencyReviewStatus.Pending : EmergencyReviewStatus.NotRequested,
            EmergencyRequestedAt = request.PossibleEmergency ? now : null
        };
        complaint.Timeline.Add(new ComplaintTimeline
        {
            UserId = reporter.Id,
            EventType = "ANONYMOUS_COMPLAINT_CREATED",
            Description = "Anonymous complaint submitted. Reporter identity is not available to operational users.",
            Timestamp = now
        });
        if (request.PossibleEmergency)
        {
            complaint.Timeline.Add(new ComplaintTimeline
            {
                UserId = reporter.Id,
                EventType = "POSSIBLE_EMERGENCY_REPORTED",
                Description = "Possible emergency flagged for official review. Critical priority has not been self-assigned.",
                Timestamp = now
            });
            complaint.EmergencyReviews.Add(_emergencyReviews.CreatePendingReview(complaint, request.EmergencyReason));
        }

        var access = new AnonymousComplaintAccess
        {
            TrackingTokenHash = Hash(trackingToken),
            TrackingExpiresAt = now.AddDays(Math.Clamp(_options.TrackingDays, 1, 180)),
            ContactEmailProtected = ProtectOptional(string.IsNullOrWhiteSpace(request.ContactEmail) ? null : request.ContactEmail.Trim().ToLowerInvariant()),
            ContactPhoneProtected = ProtectOptional(string.IsNullOrWhiteSpace(request.ContactPhone) ? null : PhoneNumberNormalizer.Normalize(request.ContactPhone)),
            ContactHint = ContactHint(request.ContactEmail, request.ContactPhone),
            CaptchaProvider = captcha.Provider,
            CaptchaScore = captcha.Score,
            SubmissionIpHash = ipHash
        };
        complaint.AnonymousAccess = access;

        var uploadedKeys = new List<string>();
        var submissionId = Guid.NewGuid().ToString("N");
        try
        {
            foreach (var item in validatedImages)
            {
                var file = item.File;
                var key = $"complaints/anonymous/{DateTime.UtcNow:yyyy/MM}/{submissionId}/{Guid.NewGuid():N}{item.Extension}";
                await using var stream = file.OpenReadStream();
                await _storage.UploadAsync(stream, key, file.ContentType, new Dictionary<string, string>
                {
                    ["submission-id"] = submissionId,
                    ["reporter-type"] = "anonymous"
                }, cancellationToken);
                uploadedKeys.Add(key);
                complaint.Images.Add(new ComplaintImage
                {
                    S3Key = key,
                    FileName = Path.GetFileName(file.FileName),
                    FileSize = file.Length,
                    MimeType = file.ContentType,
                    IsResolutionEvidence = false,
                    UploadedAt = DateTimeOffset.UtcNow
                });
            }
            await _unitOfWork.Repository<Complaint>().AddAsync(complaint, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            foreach (var key in uploadedKeys)
            {
                try { await _storage.DeleteAsync(key, cancellationToken); } catch { }
            }
            throw;
        }

        return new AnonymousComplaintCreatedResponse
        {
            ComplaintId = complaint.Id,
            ReferenceNumber = Reference(complaint),
            TrackingToken = trackingToken,
            TrackingExpiresAtUtc = access.TrackingExpiresAt,
            Status = complaint.Status.ToString(),
            Message = "Save the tracking token now. It cannot be recovered later. Anonymous complaints do not earn rewards and cannot use citizen verification or disputes."
        };
    }

    public async Task<AnonymousComplaintTrackingResponse> TrackAsync(string referenceNumber, string trackingToken, CancellationToken cancellationToken = default)
    {
        var id = ParseReference(referenceNumber);
        var complaint = await _unitOfWork.Repository<Complaint>().Query(true)
            .Include(entity => entity.Department)
            .Include(entity => entity.Ward)
            .Include(entity => entity.Timeline)
            .Include(entity => entity.AnonymousAccess)
            .FirstOrDefaultAsync(entity => entity.Id == id && entity.IsAnonymous, cancellationToken)
            ?? throw new NotFoundException("Anonymous complaint was not found.");
        var access = complaint.AnonymousAccess ?? throw new NotFoundException("Anonymous tracking record was not found.");
        if (access.TrackingExpiresAt <= DateTimeOffset.UtcNow || !FixedTimeEquals(access.TrackingTokenHash, Hash(trackingToken)))
            throw new UnauthorizedAccessException("Tracking token is invalid or expired.");

        access.LastAccessedAt = DateTimeOffset.UtcNow;
        access.AccessCount++;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new AnonymousComplaintTrackingResponse
        {
            ReferenceNumber = Reference(complaint),
            Title = complaint.Title,
            Category = complaint.Category,
            Status = complaint.Status.ToString(),
            Priority = complaint.Priority.ToString(),
            DepartmentName = complaint.Department.Name,
            WardName = complaint.Ward.Name,
            Address = complaint.Address,
            PossibleEmergency = complaint.PossibleEmergency,
            EmergencyReviewStatus = complaint.EmergencyReviewStatus.ToString(),
            ContactHint = access.ContactHint,
            CreatedAt = complaint.CreatedAt,
            UpdatedAt = complaint.UpdatedAt,
            Timeline = complaint.Timeline.OrderBy(item => item.Timestamp)
                .Select(item => new { Item = item, PublicDescription = PublicTimelineDescription(item.EventType) })
                .Where(item => item.PublicDescription is not null)
                .Select(item => new AnonymousTimelineItem
                {
                    EventType = item.Item.EventType,
                    Description = item.PublicDescription!,
                    Timestamp = item.Item.Timestamp
                }).ToArray()
        };
    }

    private async Task<User> GetOrCreateSystemReporterAsync(CancellationToken cancellationToken)
    {
        var email = _options.SystemReporterEmail.Trim().ToLowerInvariant();
        var existing = await _users.GetByEmailAsync(email, cancellationToken);
        if (existing is not null) return existing;
        await ReporterLock.WaitAsync(cancellationToken);
        try
        {
            existing = await _users.GetByEmailAsync(email, cancellationToken);
            if (existing is not null) return existing;
            var user = new User
            {
                Email = email,
                FullName = "Anonymous citizen",
                PasswordHash = _passwordHasher.Hash(GenerateToken()),
                Role = UserRole.Citizen,
                IsEmailVerified = true,
                IsActive = false,
                IsSystemAccount = true
            };
            await _users.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return user;
        }
        finally { ReporterLock.Release(); }
    }

    private async Task ValidateScopeAsync(long departmentId, long wardId, CancellationToken cancellationToken)
    {
        if (!await _unitOfWork.Repository<Department>().Query().AnyAsync(item => item.Id == departmentId && item.IsActive, cancellationToken))
            throw new ValidationException(["Select an active department."]);
        if (!await _unitOfWork.Repository<Ward>().Query().AnyAsync(item => item.Id == wardId && item.DepartmentId == departmentId && item.IsActive, cancellationToken))
            throw new ValidationException(["Select an active ward belonging to the chosen department."]);
    }

    private static void ValidateRequest(CreateAnonymousComplaintRequest request)
    {
        var errors = new List<string>();
        if (request.Title.Trim().Length is < 5 or > 200) errors.Add("Title must contain 5 to 200 characters.");
        if (request.Description.Trim().Length is < 20 or > 5000) errors.Add("Description must contain 20 to 5000 characters.");
        if (request.Address.Trim().Length is < 5 or > 500) errors.Add("Address must contain 5 to 500 characters.");
        if (request.DepartmentId <= 0 || request.WardId <= 0) errors.Add("Department and ward are required.");
        if (request.Latitude is < -90 or > 90 || request.Longitude is < -180 or > 180) errors.Add("Coordinates are invalid.");
        if (request.Images.Count > 5) errors.Add("A maximum of five images can be uploaded.");
        if ((!string.IsNullOrWhiteSpace(request.ContactEmail) || !string.IsNullOrWhiteSpace(request.ContactPhone)) && !request.ConsentToLimitedContactStorage)
            errors.Add("Consent is required before limited contact information can be stored.");
        if (!string.IsNullOrWhiteSpace(request.ContactEmail) && !MailAddress.TryCreate(request.ContactEmail.Trim(), out _))
            errors.Add("Contact email format is invalid.");
        if (!string.IsNullOrWhiteSpace(request.ContactPhone))
        {
            try { PhoneNumberNormalizer.Normalize(request.ContactPhone); }
            catch (ValidationException) { errors.Add("Contact phone format is invalid."); }
        }
        if (request.PossibleEmergency && string.IsNullOrWhiteSpace(request.EmergencyReason)) errors.Add("Explain why this may be an emergency.");
        if (string.IsNullOrWhiteSpace(request.CaptchaToken)) errors.Add("CAPTCHA verification is required.");
        if (errors.Count > 0) throw new ValidationException(errors);
    }

    private static string NormalizeCategory(string value) => Categories.FirstOrDefault(item => string.Equals(item, value.Trim(), StringComparison.OrdinalIgnoreCase))
        ?? throw new ValidationException(["Select a supported complaint category."]);

    private static string ValidateImage(string fileName, string contentType, long length)
    {
        if (length is <= 0 or > 5 * 1024 * 1024) throw new ValidationException(["Each image must be between 1 byte and 5 MB."]);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var valid = (contentType.ToLowerInvariant(), extension) switch
        {
            ("image/jpeg", ".jpg" or ".jpeg") => true,
            ("image/png", ".png") => true,
            ("image/webp", ".webp") => true,
            _ => false
        };
        if (!valid) throw new ValidationException(["Only matching JPEG, PNG and WebP images are allowed."]);
        return extension == ".jpeg" ? ".jpg" : extension;
    }


    private static string? PublicTimelineDescription(string eventType) => eventType.ToUpperInvariant() switch
    {
        "ANONYMOUS_COMPLAINT_CREATED" or "CREATED" => "Complaint submitted successfully.",
        "POSSIBLE_EMERGENCY_REPORTED" => "Possible emergency flag is awaiting official review.",
        "EMERGENCY_CONFIRMED" => "An authorized official confirmed the emergency priority.",
        "EMERGENCY_REJECTED" => "An authorized official reviewed and removed the emergency flag.",
        "AI_TRIAGE_COMPLETED" or "AI_TRIAGE" => "Automated routing and triage completed.",
        "ASSIGNED" or "REASSIGNED" or "ASSIGNMENT_ACCEPTED" or "ASSIGNMENT_REJECTED" => "The complaint assignment was updated.",
        "IN_PROGRESS" or "PROGRESS_UPDATED" => "Work progress was updated.",
        "ESCALATED" or "SLA_ESCALATED" or "SLA_BREACH" or "ADMIN_ESCALATION" => "The complaint was escalated for attention.",
        "RESOLUTION_SUBMITTED" => "Resolution evidence was submitted for review.",
        "VERIFICATION_PENDING" or "VISUAL_VERIFICATION_COMPLETED" or "VISUAL_REVIEW_RECORDED" => "Resolution evidence is under review.",
        "VISUAL_REWORK_REQUESTED" or "REWORK_REQUESTED" => "Additional work or evidence was requested.",
        "CLOSED" => "Complaint closed after review.",
        "CLOSED_AUTO" or "AUTO_CLOSED" => "Complaint closed after the verification window expired.",
        "CLOSED_FRAUD" => "Complaint closed after an official review.",
        "MERGED" => "Complaint merged with an existing issue.",
        _ => null
    };

    private string? ProtectOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : _protector.Protect(value.Trim());
    private static string ContactHint(string? email, string? phone)
    {
        if (!string.IsNullOrWhiteSpace(phone)) return PhoneNumberNormalizer.Mask(PhoneNumberNormalizer.Normalize(phone));
        if (!string.IsNullOrWhiteSpace(email))
        {
            var parts = email.Trim().Split('@');
            return parts.Length == 2 ? $"{parts[0][0]}***@{parts[1]}" : "email provided";
        }
        return "No contact information stored";
    }
    private static string Reference(Complaint complaint) => $"CH-{complaint.CreatedAt:yyyy}-{complaint.Id:D6}";
    private static long ParseReference(string reference)
    {
        var last = reference.Trim().Split('-').LastOrDefault();
        return long.TryParse(last, out var id) ? id : throw new ValidationException(["Complaint reference format is invalid."]);
    }
    private static string GenerateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static bool FixedTimeEquals(string first, string second) => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(first), Encoding.UTF8.GetBytes(second));
}
