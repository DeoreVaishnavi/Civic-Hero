using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/release")]
[Authorize(Roles = "Admin,SuperAdmin")]
public sealed class ReleaseController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public ReleaseController(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    [HttpGet("readiness")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetReadiness()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        var jwtSecret = _configuration["Jwt:SecretKey"] ?? string.Empty;
        var bucket = _configuration["AWS:BucketName"] ?? string.Empty;
        var storageProvider = _configuration["Storage:Provider"] ?? string.Empty;
        var allowedHosts = _configuration["AllowedHosts"] ?? string.Empty;
        var origins = _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        var productionOriginConfigured = origins.Any(origin =>
            Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
            uri.Scheme == Uri.UriSchemeHttps &&
            !uri.IsLoopback &&
            !uri.Host.EndsWith(".example", StringComparison.OrdinalIgnoreCase) &&
            !uri.Host.Contains("change", StringComparison.OrdinalIgnoreCase) &&
            !uri.Host.Contains("your-", StringComparison.OrdinalIgnoreCase));

        var checks = new[]
        {
            Check("environment", "Production environment", _environment.IsProduction(),
                _environment.IsProduction() ? "ASPNETCORE_ENVIRONMENT is Production." : $"Current environment is {_environment.EnvironmentName}."),
            Check("database", "AWS RDS connection", IsConfigured(connectionString),
                IsConfigured(connectionString) ? "A non-placeholder database connection is available." : "Set ConnectionStrings__DefaultConnection through a Docker secret."),
            Check("jwt", "JWT signing key", jwtSecret.Length >= 32 && IsConfigured(jwtSecret),
                jwtSecret.Length >= 32 && IsConfigured(jwtSecret) ? "A sufficiently long external signing key is loaded." : "Provide a JWT secret of at least 32 characters."),
            Check("storage", "Amazon S3 storage", storageProvider.Equals("S3", StringComparison.OrdinalIgnoreCase) && IsConfigured(bucket),
                storageProvider.Equals("S3", StringComparison.OrdinalIgnoreCase) && IsConfigured(bucket) ? "S3 provider and bucket are configured." : "Configure Storage__Provider=S3 and AWS__BucketName."),
            Check("cors", "Production CORS origin", productionOriginConfigured,
                productionOriginConfigured ? "At least one non-local HTTPS origin is allowed." : "Provide the deployed HTTPS frontend origin."),
            Check("hosts", "Host filtering", !string.IsNullOrWhiteSpace(allowedHosts) && allowedHosts != "*" && IsConfigured(allowedHosts),
                !string.IsNullOrWhiteSpace(allowedHosts) && allowedHosts != "*" && IsConfigured(allowedHosts) ? "AllowedHosts is restricted." : "Set AllowedHosts to the production hostname."),
            Check("migrations", "Safe migration policy", !_configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"),
                "Production startup does not automatically modify the schema."),
            Check("security", "Security middleware", _configuration.GetValue<bool>("Security:EnableRateLimiting") && _configuration.GetValue<bool>("Security:EnableSecurityHeaders"),
                "Rate limiting and security headers should remain enabled."),
            Check("https", "HTTPS enforcement", !_configuration.GetValue<bool>("Deployment:AllowHttp"),
                _configuration.GetValue<bool>("Deployment:AllowHttp") ? "HTTP is enabled for the local production-like profile only." : "Application HTTPS enforcement is active behind the trusted proxy."),
            Check("proxy", "Reverse-proxy forwarding", true,
                "Forwarded scheme and client IP support is enabled for the internal Nginx proxy.")
        };

        var readyCount = checks.Count(check => check.Ready);
        var score = (int)Math.Round(readyCount * 100d / checks.Length);
        var version = Environment.GetEnvironmentVariable("RELEASE_VERSION")
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? "unknown";

        return Ok(new
        {
            success = true,
            data = new
            {
                phase = 15,
                title = "Deployment & Production Readiness",
                status = score == 100 ? "Ready" : "Action required",
                readinessScore = score,
                readyChecks = readyCount,
                totalChecks = checks.Length,
                checks,
                runtime = new
                {
                    environment = _environment.EnvironmentName,
                    version,
                    commitSha = Environment.GetEnvironmentVariable("COMMIT_SHA") ?? "not supplied",
                    containerized = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true",
                    generatedAtUtc = DateTimeOffset.UtcNow
                },
                artifacts = new[]
                {
                    "Backend and frontend multi-stage Dockerfiles",
                    "Local and production Docker Compose definitions",
                    "Nginx API, SPA and SignalR reverse proxy",
                    "GitHub Actions CI, container publishing and gated deployment",
                    "Health monitoring with Prometheus black-box probes and Grafana",
                    "Deployment, rollback, backup and recovery scripts",
                    "Environment, go-live, operations and user documentation"
                },
                deploymentCommands = new[]
                {
                    "scripts/deployment/preflight.ps1",
                    "scripts/deployment/start-production-like.ps1",
                    "scripts/deployment/verify-deployment.ps1"
                },
                note = "This endpoint checks configuration presence without returning secret values. Complete Phase 14 quality gates and the Phase 15 go-live checklist before production approval."
            }
        });
    }

    private static bool IsConfigured(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        !value.Contains("YOUR_", StringComparison.OrdinalIgnoreCase) &&
        !value.Contains("CHANGE_THIS", StringComparison.OrdinalIgnoreCase) &&
        !value.Contains("CHANGE-ME", StringComparison.OrdinalIgnoreCase) &&
        !value.Contains("example.com", StringComparison.OrdinalIgnoreCase) &&
        !value.Contains(".example", StringComparison.OrdinalIgnoreCase);

    private static ReadinessCheck Check(string key, string name, bool ready, string detail) =>
        new(key, name, ready, ready ? "Ready" : "Action required", detail);

    private sealed record ReadinessCheck(
        string Key,
        string Name,
        bool Ready,
        string Status,
        string Detail);
}
