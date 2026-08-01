using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CivicHero.Backend.Core.DTOs.Security;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using CivicHero.Backend.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Core.Services;

public sealed class SecurityService : ISecurityService
{
    private readonly CivicDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRateLimitMonitor _rateLimitMonitor;
    private readonly IPasswordHasher _passwordHasher;

    public SecurityService(
        CivicDbContext db,
        ICurrentUserService currentUser,
        IHttpContextAccessor httpContextAccessor,
        IRateLimitMonitor rateLimitMonitor,
        IPasswordHasher passwordHasher)
    {
        _db = db;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
        _rateLimitMonitor = rateLimitMonitor;
        _passwordHasher = passwordHasher;
    }

    public async Task<SecuritySessionDto> GetMySessionAsync(CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUserId();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
                   ?? throw new NotFoundException("User account was not found.");
        var principal = _httpContextAccessor.HttpContext?.User;
        var sessionId = principal?.FindFirstValue("sid");
        UserSession? session = null;
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            session = await _db.UserSessions.AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId && x.SessionId == sessionId, cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        var hasCurrentSession = session is not null && !session.RevokedAt.HasValue && session.ExpiresAt > now &&
            session.AuthorizationVersion == user.AuthorizationVersion;
        var hasLegacySession = string.IsNullOrWhiteSpace(sessionId) && user.RefreshTokenHash != null && user.RefreshTokenExpiresAt > now;

        return new SecuritySessionDto(
            user.Id,
            user.Email,
            user.Role.ToString(),
            user.IsEmailVerified,
            user.IsActive,
            hasCurrentSession || hasLegacySession,
            session?.CreatedAt ?? user.RefreshTokenCreatedAt,
            session?.ExpiresAt ?? user.RefreshTokenExpiresAt,
            user.LastLoginAt,
            ReadUnixTimeClaim(principal, JwtRegisteredClaimNames.Exp),
            principal?.FindFirstValue(JwtRegisteredClaimNames.Jti),
            sessionId,
            user.AuthorizationVersion,
            DateTimeOffset.UtcNow);
    }

    public async Task<SecurityActionResultDto> ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidatePasswordChange(request);

        var userId = RequireCurrentUserId();
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
                   ?? throw new NotFoundException("User account was not found.");

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new BusinessRuleViolationException("Current password is incorrect.");

        if (_passwordHasher.Verify(request.NewPassword, user.PasswordHash))
            throw new BusinessRuleViolationException("Choose a password different from your current password.");

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        await RevokeAllSessionsAsync(user, "Password changed", cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        return new SecurityActionResultDto(user.Id, user.Email, "Password changed and sessions revoked", DateTimeOffset.UtcNow);
    }

    public async Task<IReadOnlyList<PersonalActivityDto>> GetMyActivityAsync(
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUserId();
        take = Math.Clamp(take, 1, 100);

        return await _db.AuditLogs.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .Select(x => new PersonalActivityDto(
                x.Id,
                x.Action,
                x.EntityName,
                x.EntityId,
                x.IpAddress,
                x.UserAgent,
                x.Severity,
                x.Success,
                x.HttpStatusCode,
                x.ErrorMessage,
                x.CorrelationId,
                x.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<ActiveSessionsResponseDto> GetMySessionsAsync(CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUserId();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
                   ?? throw new NotFoundException("User account was not found.");

        var now = DateTimeOffset.UtcNow;
        var currentSessionId = _httpContextAccessor.HttpContext?.User.FindFirstValue("sid");
        var sessions = await _db.UserSessions.AsNoTracking()
            .Where(x => x.UserId == userId && x.AuthorizationVersion == user.AuthorizationVersion &&
                !x.RevokedAt.HasValue && x.ExpiresAt > now)
            .OrderByDescending(x => x.LastSeenAt)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new ActiveSessionDto(
                x.SessionId,
                "Refresh token",
                x.DeviceLabel,
                x.IpAddress,
                x.UserAgent,
                x.CreatedAt,
                x.ExpiresAt,
                x.LastSeenAt,
                x.SessionId == currentSessionId,
                false))
            .ToListAsync(cancellationToken);

        return new ActiveSessionsResponseDto(
            true,
            "CivicHero stores a separate revocable refresh session for each browser or device. Up to 10 active sessions are retained per account.",
            sessions,
            now);
    }

    public async Task<SecurityActionResultDto> RevokeMySessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Session identifier is required."]);

        var userId = RequireCurrentUserId();
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
                   ?? throw new NotFoundException("User account was not found.");
        var now = DateTimeOffset.UtcNow;
        var session = await _db.UserSessions.FirstOrDefaultAsync(x => x.UserId == userId &&
            x.SessionId == sessionId.Trim() && !x.RevokedAt.HasValue && x.ExpiresAt > now &&
            x.AuthorizationVersion == user.AuthorizationVersion, cancellationToken)
            ?? throw new NotFoundException("The selected active session was not found.");

        session.RevokedAt = now;
        session.RevokedReason = "Revoked by account owner";
        await _db.SaveChangesAsync(cancellationToken);
        return new SecurityActionResultDto(user.Id, user.Email, "Selected session revoked", now);
    }

    public async Task<SecurityActionResultDto> RevokeMySessionsAsync(CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUserId();
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
                   ?? throw new NotFoundException("User account was not found.");
        await RevokeAllSessionsAsync(user, "Revoked by account owner", cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return new SecurityActionResultDto(user.Id, user.Email, "All sessions revoked", DateTimeOffset.UtcNow);
    }

    public async Task<SecurityOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var cutoff = now.AddHours(-24);
        var activeUsers = await _db.Users.AsNoTracking().CountAsync(x => x.IsActive, cancellationToken);
        var activeSessions = await _db.UserSessions.AsNoTracking().CountAsync(x => !x.RevokedAt.HasValue &&
            x.ExpiresAt > now && x.AuthorizationVersion == x.User.AuthorizationVersion && x.User.IsActive, cancellationToken);
        var lockedAccounts = await _db.Users.AsNoTracking().CountAsync(x => x.LockoutEnd > now, cancellationToken);
        var withFailures = await _db.Users.AsNoTracking().CountAsync(x => x.FailedLoginAttempts > 0, cancellationToken);
        var failedWrites = await _db.AuditLogs.AsNoTracking().CountAsync(x => !x.Success && x.CreatedAt >= cutoff, cancellationToken);
        var authenticationFailures = await _db.AuditLogs.AsNoTracking().CountAsync(x => !x.Success && x.CreatedAt >= cutoff && x.EntityName == "Auth", cancellationToken);
        var distinctAddresses = await _db.AuditLogs.AsNoTracking()
            .Where(x => !x.Success && x.CreatedAt >= cutoff && x.IpAddress != null)
            .Select(x => x.IpAddress!)
            .Distinct()
            .CountAsync(cancellationToken);
        return new SecurityOverviewDto(activeUsers, activeSessions, lockedAccounts, withFailures,
            failedWrites, authenticationFailures, distinctAddresses, _rateLimitMonitor.Snapshot(), now);
    }

    public async Task<IReadOnlyList<SecurityEventDto>> GetEventsAsync(int take = 50, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 200);
        return await _db.AuditLogs.AsNoTracking()
            .Where(x => !x.Success || x.EntityName == "Auth" || x.EntityName == "Security")
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .Select(x => new SecurityEventDto(x.Id, x.UserId, x.UserEmail, x.UserRole, x.Action,
                x.EntityName, x.IpAddress, x.Severity, x.Success, x.HttpStatusCode,
                x.ErrorMessage, x.CorrelationId, x.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LockedAccountDto>> GetLockedAccountsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        return await _db.Users.AsNoTracking()
            .Where(x => x.LockoutEnd > now || x.FailedLoginAttempts > 0)
            .OrderByDescending(x => x.LockoutEnd)
            .ThenByDescending(x => x.FailedLoginAttempts)
            .Select(x => new LockedAccountDto(x.Id, x.FullName, x.Email, x.Role.ToString(),
                x.FailedLoginAttempts, x.LockoutEnd, x.LastLoginAt, x.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<SecurityActionResultDto> UnlockAccountAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await LoadManagedUserAsync(userId, cancellationToken);
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        await _db.SaveChangesAsync(cancellationToken);
        return new SecurityActionResultDto(user.Id, user.Email, "Account unlocked", DateTimeOffset.UtcNow);
    }

    public async Task<SecurityActionResultDto> RevokeUserSessionsAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await LoadManagedUserAsync(userId, cancellationToken);
        await RevokeAllSessionsAsync(user, $"Revoked by {_currentUser.Role ?? "administrator"}", cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return new SecurityActionResultDto(user.Id, user.Email, "User sessions revoked", DateTimeOffset.UtcNow);
    }

    private async Task RevokeAllSessionsAsync(User user, string reason, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var active = await _db.UserSessions
            .Where(x => x.UserId == user.Id && !x.RevokedAt.HasValue && x.ExpiresAt > now)
            .ToListAsync(cancellationToken);
        foreach (var session in active)
        {
            session.RevokedAt = now;
            session.RevokedReason = reason;
        }

        user.RefreshTokenHash = null;
        user.RefreshTokenCreatedAt = null;
        user.RefreshTokenExpiresAt = null;
        user.AuthorizationVersion = checked(user.AuthorizationVersion + 1);
    }

    private async Task<User> LoadManagedUserAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
                   ?? throw new NotFoundException("User account was not found.");
        if (user.Role == UserRole.SuperAdmin && !string.Equals(_currentUser.Role, UserRole.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Only a SuperAdmin may manage another SuperAdmin session.");
        return user;
    }

    private long RequireCurrentUserId() => _currentUser.UserId
        ?? throw new UnauthorizedAccessException("Authenticated user identifier is missing.");

    private static void ValidatePasswordChange(ChangePasswordRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            errors.Add("Current password is required.");
        else if (request.CurrentPassword.Length > 128)
            errors.Add("Current password cannot exceed 128 characters.");

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8 || request.NewPassword.Length > 128)
            errors.Add("New password must be between 8 and 128 characters.");
        else
        {
            if (!request.NewPassword.Any(char.IsUpper)) errors.Add("New password must contain an uppercase letter.");
            if (!request.NewPassword.Any(char.IsLower)) errors.Add("New password must contain a lowercase letter.");
            if (!request.NewPassword.Any(char.IsDigit)) errors.Add("New password must contain a number.");
            if (!request.NewPassword.Any(character => !char.IsLetterOrDigit(character)))
                errors.Add("New password must contain a special character.");
        }

        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
            errors.Add("New password and confirmation password must match.");

        if (errors.Count > 0)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(errors);
    }

    private static DateTimeOffset? ReadUnixTimeClaim(ClaimsPrincipal? principal, string claimType)
    {
        var raw = principal?.FindFirstValue(claimType);
        return long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? DateTimeOffset.FromUnixTimeSeconds(value)
            : null;
    }
}
