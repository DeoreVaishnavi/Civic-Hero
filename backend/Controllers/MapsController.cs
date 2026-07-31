using CivicHero.Backend.Infrastructure.Configurations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/maps")]
[Authorize]
public sealed class MapsController : ControllerBase
{
    private readonly MapboxOptions _options;

    public MapsController(IOptionsSnapshot<MapboxOptions> options)
    {
        _options = options.Value;
    }

    [HttpGet("config")]
    public IActionResult GetConfig()
    {
        var configured = _options.IsConfigured;
        return Ok(new
        {
            success = true,
            message = configured
                ? "Map configuration loaded."
                : "Mapbox is not configured. CivicHero will use the built-in geographic fallback view.",
            data = new
            {
                enabled = configured,
                provider = "Mapbox",
                accessToken = configured ? _options.AccessToken : null,
                styleUrl = _options.StyleUrl,
                defaultCenter = new
                {
                    latitude = _options.DefaultLatitude,
                    longitude = _options.DefaultLongitude
                },
                defaultZoom = _options.DefaultZoom
            }
        });
    }
}
