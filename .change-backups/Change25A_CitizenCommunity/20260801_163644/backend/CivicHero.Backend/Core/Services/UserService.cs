using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using CivicHero.Backend.Core.DTOs.Common;
using CivicHero.Backend.Core.DTOs.Users;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Core.Validation;
using CivicHero.Backend.Infrastructure.Security;
using CivicHero.Backend.Infrastructure.Storage;

namespace CivicHero.Backend.Core.Services;

public sealed class UserService : IUserService
{
    private const long MaximumAvatarBytes = 5 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, string> AvatarExtensions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = "jpg",
            ["image/png"] = "png",
            ["image/webp"] = "webp"
        };

    private readonly IUserRepository _users;
    private readonly IDepartmentRepository _departments;
    private readonly IWardRepository _wards;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IStorageService _storage;
    private readonly IHostEnvironment _environment;

    public UserService(
        IUserRepository users,
        IDepartmentRepository departments,
        IWardRepository wards,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IPasswordHasher passwordHasher,
        IStorageService storage,
        IHostEnvironment environment)
    {
        _users = users;
        _departments = departments;
        _wards = wards;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _passwordHasher = passwordHasher;
        _storage = storage;
        _environment = environment;
    }

    public async Task<UserProfileDto> GetProfileAsync(long userId, CancellationToken cancellationToken = default) =>
        _mapper.Map<UserProfileDto>(await GetUserEntityAsync(userId, false, cancellationToken));

    public async Task<UserProfileDto> UpdateProfileAsync(long userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetUserEntityAsync(userId, true, cancellationToken);
        ApplyProfile(user, request);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetProfileAsync(user.Id, cancellationToken);
    }

    public async Task<ChangeEmailResponse> ChangeEmailAsync(
        long userId,
        ChangeEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserEntityAsync(userId, true, cancellationToken);
        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new BusinessRuleViolationException("Current password is incorrect.");

        var newEmail = NormalizeEmail(request.NewEmail);
        if (string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleViolationException("The new email address must be different from the current email address.");

        var existing = await _users.GetByEmailAsync(newEmail, cancellationToken);
        if (existing is not null && existing.Id != user.Id)
            throw new ConflictException("An account with this email already exists.");

        var verificationToken = GenerateSecureToken();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);
        user.Email = newEmail;
        user.IsEmailVerified = false;
        user.EmailVerificationTokenHash = HashToken(verificationToken);
        user.EmailVerificationTokenExpiresAt = expiresAt;
        InvalidateSessions(user);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new ChangeEmailResponse
        {
            Email = newEmail,
            VerificationExpiresAtUtc = expiresAt,
            DevelopmentVerificationToken = _environment.IsDevelopment() ? verificationToken : null
        };
    }

    public async Task<ProfileAvatarResult> UploadAvatarAsync(
        long userId,
        UploadProfileAvatarRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = await GetUserEntityAsync(userId, false, cancellationToken);
        var file = request.File ?? throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Select an avatar image."]);

        if (file.Length <= 0 || file.Length > MaximumAvatarBytes)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Avatar image must be between 1 byte and 5 MB."]);

        if (!AvatarExtensions.TryGetValue(file.ContentType, out var extension))
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Only JPEG, PNG and WebP avatar images are allowed."]);

        await using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        if (!HasExpectedImageSignature(buffer.GetBuffer().AsSpan(0, checked((int)buffer.Length)), file.ContentType))
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["The uploaded avatar content does not match its image type."]);

        buffer.Position = 0;
        await _storage.UploadAsync(
            buffer,
            AvatarObjectKey(userId, extension),
            file.ContentType,
            new Dictionary<string, string>
            {
                ["user-id"] = userId.ToString(),
                ["purpose"] = "profile-avatar"
            },
            cancellationToken);
        await DeleteAvatarObjectsAsync(userId, cancellationToken, extension);

        return new ProfileAvatarResult(true, "Avatar uploaded", DateTimeOffset.UtcNow);
    }

    public async Task<ProfileAvatarContent?> GetAvatarAsync(long userId, CancellationToken cancellationToken = default)
    {
        _ = await GetUserEntityAsync(userId, false, cancellationToken);
        foreach (var extension in AvatarExtensions.Values.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var fileName = $"avatar.{extension}";
            var download = await _storage.DownloadAsync(AvatarObjectKey(userId, extension), fileName, cancellationToken);
            if (download is not null)
                return new ProfileAvatarContent(download.Content, download.ContentType, download.FileName);
        }

        return null;
    }

    public async Task<ProfileAvatarResult> DeleteAvatarAsync(long userId, CancellationToken cancellationToken = default)
    {
        _ = await GetUserEntityAsync(userId, false, cancellationToken);
        await DeleteAvatarObjectsAsync(userId, cancellationToken);
        return new ProfileAvatarResult(false, "Avatar removed", DateTimeOffset.UtcNow);
    }

    public async Task<PagedResponse<UserListDto>> GetUsersAsync(UserQuery query, CancellationToken cancellationToken = default)
    {
        query.Page = Math.Max(1, query.Page);
        query.PageSize = Math.Clamp(query.PageSize, 1, 100);
        var result = await _users.GetPagedAsync(query, cancellationToken);
        return new PagedResponse<UserListDto>
        {
            Items = result.Items.Select(item => _mapper.Map<UserListDto>(item)).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = result.TotalCount
        };
    }

    public async Task<UserProfileDto> GetUserAsync(long requesterId, string requesterRole, long targetUserId, CancellationToken cancellationToken = default)
    {
        EnsureSelfOrAdmin(requesterId, requesterRole, targetUserId);
        return await GetProfileAsync(targetUserId, cancellationToken);
    }

    public async Task<UserProfileDto> UpdateUserAsync(long requesterId, string requesterRole, long targetUserId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        EnsureSelfOrAdmin(requesterId, requesterRole, targetUserId);
        return await UpdateProfileAsync(targetUserId, request, cancellationToken);
    }

    public async Task<UserProfileDto> ChangeRoleAsync(long requesterId, string requesterRole, long targetUserId, ChangeRoleRequest request, CancellationToken cancellationToken = default)
    {
        var actorRole = ParseRole(requesterRole);
        var target = await GetUserEntityAsync(targetUserId, true, cancellationToken);
        EnsureCanManage(actorRole, requesterId, target);
        var newRole = ParseRole(request.Role);

        if (newRole == UserRole.SuperAdmin && actorRole != UserRole.SuperAdmin)
            throw new UnauthorizedAccessException("Only a SuperAdmin can assign the SuperAdmin role.");

        if (actorRole == UserRole.Admin
            && newRole != target.Role
            && newRole is UserRole.Officer or UserRole.Supervisor)
            throw new UnauthorizedAccessException("Use Staff accounts to create Officer or Supervisor users. SuperAdmin approval is required.");

        target.Role = newRole;
        target.DepartmentId = request.DepartmentId;
        target.WardId = request.WardId;
        await NormalizeAndValidateScopeAsync(target, cancellationToken);
        InvalidateSessions(target);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetProfileAsync(target.Id, cancellationToken);
    }

    public async Task<UserProfileDto> AssignDepartmentAsync(long requesterId, string requesterRole, long targetUserId, AssignDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        var actorRole = ParseRole(requesterRole);
        var target = await GetUserEntityAsync(targetUserId, true, cancellationToken);
        EnsureCanManage(actorRole, requesterId, target);
        target.DepartmentId = request.DepartmentId;
        if (!request.DepartmentId.HasValue) target.WardId = null;
        await NormalizeAndValidateScopeAsync(target, cancellationToken);
        InvalidateSessions(target);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetProfileAsync(target.Id, cancellationToken);
    }

    public async Task<UserProfileDto> AssignWardAsync(long requesterId, string requesterRole, long targetUserId, AssignWardRequest request, CancellationToken cancellationToken = default)
    {
        var actorRole = ParseRole(requesterRole);
        var target = await GetUserEntityAsync(targetUserId, true, cancellationToken);
        EnsureCanManage(actorRole, requesterId, target);
        target.WardId = request.WardId;
        await NormalizeAndValidateScopeAsync(target, cancellationToken);
        InvalidateSessions(target);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetProfileAsync(target.Id, cancellationToken);
    }

    public async Task<UserProfileDto> SetActiveAsync(long requesterId, string requesterRole, long targetUserId, bool isActive, CancellationToken cancellationToken = default)
    {
        var actorRole = ParseRole(requesterRole);
        var target = await GetUserEntityAsync(targetUserId, true, cancellationToken);
        EnsureCanManage(actorRole, requesterId, target);
        if (requesterId == targetUserId && !isActive)
            throw new BusinessRuleViolationException("You cannot deactivate your own account.");
        if (isActive
            && actorRole == UserRole.Admin
            && target.Role is UserRole.Officer or UserRole.Supervisor)
            throw new UnauthorizedAccessException("Only a SuperAdmin can activate or approve an Officer or Supervisor account.");
        target.IsActive = isActive;
        InvalidateSessions(target);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetProfileAsync(target.Id, cancellationToken);
    }

    public async Task ForceLogoutAsync(long requesterId, string requesterRole, long targetUserId, CancellationToken cancellationToken = default)
    {
        var actorRole = ParseRole(requesterRole);
        var target = await GetUserEntityAsync(targetUserId, true, cancellationToken);
        EnsureCanManage(actorRole, requesterId, target);
        InvalidateSessions(target);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserManagementMetadataDto> GetManagementMetadataAsync(CancellationToken cancellationToken = default)
    {
        var departments = (await _departments.ListAsync(cancellationToken)).Where(x => x.IsActive).OrderBy(x => x.Name).ToList();
        var wards = (await _wards.ListAsync(cancellationToken)).Where(x => x.IsActive).OrderBy(x => x.Name).ToList();
        return new UserManagementMetadataDto
        {
            Roles = Enum.GetNames<UserRole>(),
            Departments = departments.Select(x => new LookupDto { Id = x.Id, Name = x.Name, Code = x.Code }).ToList(),
            Wards = wards.Select(x => new WardLookupDto { Id = x.Id, DepartmentId = x.DepartmentId, Name = x.Name, Code = x.Code }).ToList()
        };
    }

    private async Task DeleteAvatarObjectsAsync(
        long userId,
        CancellationToken cancellationToken,
        string? exceptExtension = null)
    {
        foreach (var extension in AvatarExtensions.Values.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.Equals(extension, exceptExtension, StringComparison.OrdinalIgnoreCase)) continue;
            await _storage.DeleteAsync(AvatarObjectKey(userId, extension), cancellationToken);
        }
    }

    private static string AvatarObjectKey(long userId, string extension) =>
        $"profiles/{userId}/avatar.{extension}";

    private static bool HasExpectedImageSignature(ReadOnlySpan<byte> bytes, string contentType) =>
        contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
            "image/png" => bytes.Length >= 8
                && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47
                && bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A,
            "image/webp" => bytes.Length >= 12
                && bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F'
                && bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P',
            _ => false
        };

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    private static string GenerateSecureToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');
    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private async Task<User> GetUserEntityAsync(long id, bool tracking, CancellationToken cancellationToken) =>
        await _users.GetProfileByIdAsync(id, tracking, cancellationToken)
        ?? throw new NotFoundException("User account was not found.");

    private static void ApplyProfile(User user, UpdateProfileRequest request)
    {
        user.FullName = PersonNameRules.Normalize(request.FullName);
        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
    }

    private async Task NormalizeAndValidateScopeAsync(User target, CancellationToken cancellationToken)
    {
        if (target.Role is UserRole.Citizen or UserRole.Admin or UserRole.SuperAdmin)
        {
            target.DepartmentId = null;
            target.WardId = null;
            return;
        }

        if (!target.DepartmentId.HasValue)
            throw new BusinessRuleViolationException("Officer and Supervisor accounts require a department.");

        var department = await _departments.GetByIdAsync(target.DepartmentId.Value, cancellationToken)
            ?? throw new NotFoundException("Selected department was not found.");
        if (!department.IsActive)
            throw new BusinessRuleViolationException("Selected department is inactive.");

        if (target.Role == UserRole.Officer && !target.WardId.HasValue)
            throw new BusinessRuleViolationException("Officer accounts require a ward.");

        if (target.WardId.HasValue)
        {
            var ward = await _wards.GetByIdAsync(target.WardId.Value, cancellationToken)
                ?? throw new NotFoundException("Selected ward was not found.");
            if (!ward.IsActive || ward.DepartmentId != target.DepartmentId.Value)
                throw new BusinessRuleViolationException("Selected ward must be active and belong to the selected department.");
        }
    }

    private static void EnsureSelfOrAdmin(long requesterId, string requesterRole, long targetUserId)
    {
        var role = ParseRole(requesterRole);
        if (requesterId != targetUserId && role is not UserRole.Admin and not UserRole.SuperAdmin)
            throw new UnauthorizedAccessException("You do not have permission to access this user.");
    }

    private static void EnsureCanManage(UserRole actorRole, long requesterId, User target)
    {
        if (actorRole is not UserRole.Admin and not UserRole.SuperAdmin)
            throw new UnauthorizedAccessException("Administrator permission is required.");
        if (target.Role == UserRole.SuperAdmin && actorRole != UserRole.SuperAdmin)
            throw new UnauthorizedAccessException("Only a SuperAdmin can manage another SuperAdmin.");
        if (requesterId == target.Id && target.Role == UserRole.SuperAdmin && actorRole == UserRole.SuperAdmin)
            return;
    }

    private static UserRole ParseRole(string role) =>
        Enum.TryParse<UserRole>(role, true, out var parsed)
            ? parsed
            : throw new UnauthorizedAccessException("Authenticated role is invalid.");

    private static void InvalidateSessions(User user)
    {
        user.AuthorizationVersion++;
        user.RefreshTokenHash = null;
        user.RefreshTokenCreatedAt = null;
        user.RefreshTokenExpiresAt = null;
    }
}
