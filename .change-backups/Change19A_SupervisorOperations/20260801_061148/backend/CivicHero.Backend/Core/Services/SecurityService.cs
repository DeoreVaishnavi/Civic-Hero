using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CivicHero.Backend.Core.Constants;
using CivicHero.Backend.Core.DTOs.Security;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Core.Services;

public sealed class SecurityService : ISecurityService
{
    private readonly CivicDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRateLimitMonitor _rateLimitMonitor;

    public SecurityService(
        CivicDbContext db,
        ICurrentUserService currentUser,
        IHttpContextAccessor httpContextAccessor,
        IRateLimitMonitor rateLimitMonitor)
    {
        _db = db;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
        _rateLimitMonitor = rateLimitMonitor;
    }

    public async Task<SecuritySessionDto> GetMySessionAsync(CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUserId();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
                   ?? throw new NotFoundException("User account was not found.");
        var principal = _httpContextAccessor.HttpContext?.User;
        return new SecuritySessionDto(
            user.Id,
            user.Email,
            user.Role.ToString(),
            user.IsEmailVerified,
            user.IsActive,
            user.RefreshTokenHash != null && user.RefreshTokenExpiresAt > DateTimeOffset.UtcNow,
            user.RefreshTokenCreatedAt,
            user.RefreshTokenExpiresAt,
            user.LastLoginAt,
            ReadUnixTimeClaim(principal, JwtRegisteredClaimNames.Exp),
            principal?.FindFirstValue(JwtRegisteredClaimNames.Jti),
            user.AuthorizationVersion,
            DateTimeOffset.UtcNow);
    }

    public async Task<SecurityActionResultDto> RevokeMySessionsAsync(CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUserId();
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
                   ?? throw new NotFoundException("User account was not found.");
        RevokeSessions(user);
        await _db.SaveChangesAsync(cancellationToken);
        return new SecurityActionResultDto(user.Id, user.Email, "All sessions revoked", DateTimeOffset.UtcNow);
    }

    public async Task<SecurityOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var cutoff = now.AddHours(-24);
        var activeUsers = await _db.Users.AsNoTracking().CountAsync(x => x.IsActive, cancellationToken);
        var activeSessions = await _db.Users.AsNoTracking().CountAsync(x => x.RefreshTokenHash != null && x.RefreshTokenExpiresAt > now, cancellationToken);
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
        RevokeSessions(user);
        await _db.SaveChangesAsync(cancellationToken);
        return new SecurityActionResultDto(user.Id, user.Email, "User sessions revoked", DateTimeOffset.UtcNow);
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

    private static void RevokeSessions(User user)
    {
        user.RefreshTokenHash = null;
        user.RefreshTokenCreatedAt = null;
        user.RefreshTokenExpiresAt = null;
        user.AuthorizationVersion = checked(user.AuthorizationVersion + 1);
    }

    private static DateTimeOffset? ReadUnixTimeClaim(ClaimsPrincipal? principal, string claimType)
    {
        var raw = principal?.FindFirstValue(claimType);
        return long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? DateTimeOffset.FromUnixTimeSeconds(value)
            : null;
    }
}
