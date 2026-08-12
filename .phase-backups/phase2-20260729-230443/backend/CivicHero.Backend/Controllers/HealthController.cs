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
                correlationId = HttpContext.TraceIdentifier,
                dependenciesEndpoint = "/api/v1/health/dependencies"
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

    [HttpGet("dependencies")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> GetDependencies(CancellationToken cancellationToken) =>
        BuildDependencyResponseAsync(
            registration => registration.Tags.Contains("ready"),
            "CivicHero dependencies checked.",
            cancellationToken);

    [HttpGet("database")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> GetDatabase(CancellationToken cancellationToken) =>
        BuildDependencyResponseAsync(
            registration => registration.Name == "database",
            "AWS RDS MySQL checked.",
            cancellationToken);

    [HttpGet("storage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> GetStorage(CancellationToken cancellationToken) =>
        BuildDependencyResponseAsync(
            registration => registration.Name == "storage",
            "Amazon S3 checked.",
            cancellationToken);

    private async Task<IActionResult> BuildDependencyResponseAsync(
        Func<HealthCheckRegistration, bool> predicate,
        string message,
        CancellationToken cancellationToken)
    {
        var report = await _healthCheckService.CheckHealthAsync(predicate, cancellationToken);
        var data = new
        {
            status = report.Status.ToString(),
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 2)
            })
        };

        var response = new
        {
            success = report.Status == HealthStatus.Healthy,
            message,
            data,
            traceId = HttpContext.TraceIdentifier
        };

        return report.Status == HealthStatus.Healthy
            ? Ok(response)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }
}
