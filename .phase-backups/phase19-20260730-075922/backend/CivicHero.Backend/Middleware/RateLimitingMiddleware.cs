using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Json;
using CivicHero.Backend.Core.Services;
using CivicHero.Backend.Infrastructure.Configurations;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Middleware;

public sealed class RateLimitingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<SecurityOptions> _options;
    private readonly IRateLimitMonitor _monitor;
    private readonly ConcurrentDictionary<string, RequestWindow> _windows = new(StringComparer.Ordinal);
    private long _cleanupCounter;

    public RateLimitingMiddleware(
        RequestDelegate next,
        IOptionsMonitor<SecurityOptions> options,
        IRateLimitMonitor monitor)
    {
        _next = next;
        _options = options;
        _monitor = monitor;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var options = _options.CurrentValue;
        if (!options.EnableRateLimiting || ShouldBypass(context.Request))
        {
            await _next(context);
            return;
        }

        var (policyName, policy) = ResolvePolicy(context.Request.Path, options);
        var permitLimit = Math.Clamp(policy.PermitLimit, 1, 10000);
        var windowSeconds = Math.Clamp(policy.WindowSeconds, 1, 3600);
        var now = DateTimeOffset.UtcNow;
        var identity = ResolveIdentity(context);
        var key = $"{policyName}:{identity}";
        var window = _windows.GetOrAdd(key, _ => new RequestWindow(now));

        bool accepted;
        int remaining;
        int retryAfterSeconds;
        lock (window.SyncRoot)
        {
            if ((now - window.StartedAtUtc).TotalSeconds >= windowSeconds)
            {
                window.StartedAtUtc = now;
                window.Count = 0;
            }

            accepted = window.Count < permitLimit;
            if (accepted) window.Count++;
            remaining = Math.Max(0, permitLimit - window.Count);
            retryAfterSeconds = Math.Max(1, windowSeconds - (int)(now - window.StartedAtUtc).TotalSeconds);
        }

        context.Response.Headers["X-RateLimit-Limit"] = permitLimit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
        context.Response.Headers["X-RateLimit-Policy"] = policyName;

        if (!accepted)
        {
            _monitor.RecordRejected(policyName);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "application/json";
            context.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                success = false,
                message = "Too many requests. Please retry after the indicated delay.",
                retryAfterSeconds,
                traceId = context.TraceIdentifier
            }, JsonOptions), context.RequestAborted);
            return;
        }

        _monitor.RecordAllowed(policyName);
        CleanupExpiredWindows(now, windowSeconds);
        await _next(context);
    }

    private static bool ShouldBypass(HttpRequest request) =>
        HttpMethods.IsOptions(request.Method) ||
        request.Path.StartsWithSegments("/health") ||
        request.Path.StartsWithSegments("/swagger");

    private static (string Name, RateLimitPolicyOptions Policy) ResolvePolicy(PathString path, SecurityOptions options)
    {
        if (path.StartsWithSegments("/api/v1/auth")) return ("authentication", options.Authentication);
        if (path.StartsWithSegments("/api/v1/admin") || path.StartsWithSegments("/api/v1/security/admin"))
            return ("administration", options.Administration);
        if (path.StartsWithSegments("/api/v1/files") ||
            (path.StartsWithSegments("/api/v1/complaints") && path.Value?.Contains("images", StringComparison.OrdinalIgnoreCase) == true) ||
            (path.StartsWithSegments("/api/v1/assignments") && path.Value?.Contains("resolve", StringComparison.OrdinalIgnoreCase) == true))
            return ("uploads", options.Uploads);
        return ("general", options.General);
    }

    private static string ResolveIdentity(HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId)) return $"user:{userId}";
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim();
        var address = string.IsNullOrWhiteSpace(forwarded)
            ? context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
            : forwarded;
        return $"ip:{address}";
    }

    private void CleanupExpiredWindows(DateTimeOffset now, int currentWindowSeconds)
    {
        if (Interlocked.Increment(ref _cleanupCounter) % 512 != 0) return;
        var cutoff = now.AddSeconds(-Math.Max(currentWindowSeconds * 2, 600));
        foreach (var item in _windows)
            if (item.Value.StartedAtUtc < cutoff) _windows.TryRemove(item.Key, out _);
    }

    private sealed class RequestWindow
    {
        public RequestWindow(DateTimeOffset startedAtUtc) => StartedAtUtc = startedAtUtc;
        public object SyncRoot { get; } = new();
        public DateTimeOffset StartedAtUtc { get; set; }
        public int Count { get; set; }
    }
}
