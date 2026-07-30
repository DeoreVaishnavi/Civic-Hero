using CivicHero.Backend.Infrastructure.Configurations;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<SecurityOptions> _options;

    public SecurityHeadersMiddleware(RequestDelegate next, IOptionsMonitor<SecurityOptions> options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_options.CurrentValue.EnableSecurityHeaders)
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers["Permissions-Policy"] = "camera=(self), geolocation=(self), microphone=()";
                headers["Cross-Origin-Opener-Policy"] = "same-origin";
                headers["X-Permitted-Cross-Domain-Policies"] = "none";
                headers.Remove("Server");

                if (!context.Request.Path.StartsWithSegments("/swagger"))
                    headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";

                if (context.Request.Path.StartsWithSegments("/api/v1/auth") ||
                    context.Request.Path.StartsWithSegments("/api/v1/security"))
                    headers["Cache-Control"] = "no-store, max-age=0";

                return Task.CompletedTask;
            });
        }

        await _next(context);
    }
}
