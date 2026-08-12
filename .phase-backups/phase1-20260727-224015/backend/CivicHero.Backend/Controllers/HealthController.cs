using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/health")]
public sealed class HealthController : ControllerBase
{
    private readonly IHostEnvironment _environment;

    public HealthController(IHostEnvironment environment)
    {
        _environment = environment;
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
                version = "1.0.0-phase0",
                environment = _environment.EnvironmentName,
                serverTimeUtc = DateTimeOffset.UtcNow
            }
        });
    }
}
