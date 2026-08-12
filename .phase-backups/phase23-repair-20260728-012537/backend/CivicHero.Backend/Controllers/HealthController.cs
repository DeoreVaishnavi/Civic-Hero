using CivicHero.Backend.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/health")]
public sealed class HealthController : ControllerBase
{
    private readonly IHostEnvironment _environment;
    private readonly HealthCheckService _healthCheckService;

    public HealthController(
        IHostEnvironment environment,
        HealthCheckService healthCheckService)
    {
        _environment = environment;
        _healthCheckService = healthCheckService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            success = true,
            message = "CivicHero API is running",
            data = new
            {
                status = "Healthy",
                service = "CivicHero.Backend",
                version = "1.0.0-phase2",
                apiVersion = "v1",
                environment = _environment.EnvironmentName,
                serverTimeUtc = DateTimeOffset.UtcNow,
                correlationId = HttpContext.TraceIdentifier
            }
        });
    }

    [HttpGet("headers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHeaderCheck()
    {
        return Ok(new
        {
            success = true,
            message = "Correlation header received successfully",
            data = new
            {
                header = CorrelationIdMiddleware.HeaderName,
                correlationId = HttpContext.TraceIdentifier
            }
        });
    }

    [HttpGet("infrastructure")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInfrastructureHealth(
        CancellationToken cancellationToken)
    {
        var report = await _healthCheckService.CheckHealthAsync(
            registration => registration.Tags.Contains("ready"),
            cancellationToken);

        return Ok(ToResponse(report, "Infrastructure health check completed"));
    }

    [HttpGet("database")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDatabaseHealth(
        CancellationToken cancellationToken)
    {
        var report = await _healthCheckService.CheckHealthAsync(
            registration => registration.Tags.Contains("database"),
            cancellationToken);

        return Ok(ToResponse(report, "AWS RDS MySQL health check completed"));
    }

    [HttpGet("storage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStorageHealth(
        CancellationToken cancellationToken)
    {
        var report = await _healthCheckService.CheckHealthAsync(
            registration => registration.Tags.Contains("storage"),
            cancellationToken);

        return Ok(ToResponse(report, "Amazon S3 health check completed"));
    }

    private object ToResponse(HealthReport report, string message)
    {
        var services = report.Entries.ToDictionary(
            entry => entry.Key,
            entry => new
            {
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 2),
                data = entry.Value.Data
            });

        return new
        {
            success = report.Status != HealthStatus.Unhealthy,
            message,
            data = new
            {
                status = report.Status.ToString(),
                totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
                services,
                correlationId = HttpContext.TraceIdentifier
            }
        };
    }
}
