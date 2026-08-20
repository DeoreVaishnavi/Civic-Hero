using System.Security.Claims;
using System.Text.Json;
using CivicHero.Backend.Core.DTOs.Administration;
using CivicHero.Backend.Core.Services;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CivicHero.Backend.Middleware;

public sealed class GovernanceRolePolicyMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly RequestDelegate _next;

    public GovernanceRolePolicyMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, CivicDbContext db, IMemoryCache cache)
    {
        if (context.User.Identity?.IsAuthenticated != true ||
            !context.Request.Path.StartsWithSegments("/api/v1", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var role = context.User.FindFirstValue(ClaimTypes.Role);
        if (string.IsNullOrWhiteSpace(role))
        {
            await _next(context);
            return;
        }

        var policy = await cache.GetOrCreateAsync(SuperAdminGovernanceService.RolePolicyCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
            var setting = await db.SystemSettings.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Key == SuperAdminGovernanceService.RolePolicyKey, context.RequestAborted);
            if (setting is null) return null;
            try { return JsonSerializer.Deserialize<RolePolicyConfigurationDto>(setting.Value, JsonOptions); }
            catch (JsonException) { return null; }
        });

        var rolePolicy = policy?.Roles?.FirstOrDefault(x => string.Equals(x.Role, role, StringComparison.OrdinalIgnoreCase));
        if (rolePolicy is null)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value?.TrimEnd('/').ToLowerInvariant() ?? string.Empty;
        var deniedPrefixes = rolePolicy.DeniedApiPrefixes ?? Array.Empty<string>();
        var denied = !rolePolicy.Enabled || deniedPrefixes.Any(prefix =>
        {
            var normalized = prefix.Trim().TrimEnd('/').ToLowerInvariant();
            return normalized.Length > 0 && (path == normalized || path.StartsWith(normalized + "/", StringComparison.Ordinal));
        });

        if (!denied)
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            success = false,
            message = rolePolicy.Enabled
                ? "This API area is denied by the active global role policy."
                : "This role is temporarily disabled by the active global role policy."
        }, context.RequestAborted);
    }
}
