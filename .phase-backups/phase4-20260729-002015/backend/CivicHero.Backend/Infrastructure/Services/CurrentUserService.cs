using System.Security.Claims;
using CivicHero.Backend.Core.Interfaces;

namespace CivicHero.Backend.Infrastructure.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public long? UserId => long.TryParse(
        Principal?.FindFirstValue(ClaimTypes.NameIdentifier),
        out var userId) ? userId : null;

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);
    public string? Role => Principal?.FindFirstValue(ClaimTypes.Role);
}
