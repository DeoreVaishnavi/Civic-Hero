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

    public long? UserId => ParseLongClaim(ClaimTypes.NameIdentifier);
    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);
    public string? Role => Principal?.FindFirstValue(ClaimTypes.Role);
    public long? DepartmentId => ParseLongClaim("DepartmentId");
    public long? WardId => ParseLongClaim("WardId");

    private long? ParseLongClaim(string claimType) =>
        long.TryParse(Principal?.FindFirstValue(claimType), out var value) ? value : null;
}
