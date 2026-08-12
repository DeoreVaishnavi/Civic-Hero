using CivicHero.Backend.Core.Constants;
using CivicHero.Backend.Core.DTOs.Rewards;
using CivicHero.Backend.Core.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/rewards")]
public sealed class RewardsController : ControllerBase
{
    private readonly IRewardService _service;
    private readonly IServiceProvider _services;

    public RewardsController(IRewardService service, IServiceProvider services)
    {
        _service = service;
        _services = services;
    }

    [HttpGet("leaderboard")]
    [AllowAnonymous]
    public async Task<IActionResult> Leaderboard([FromQuery] int limit = 25, CancellationToken cancellationToken = default) =>
        OkEnvelope("Leaderboard loaded.", await _service.GetLeaderboardAsync(limit, cancellationToken));

    [HttpGet("points")]
    [Authorize(Policy = PermissionConstants.CitizenOnly)]
    public async Task<IActionResult> Points(CancellationToken cancellationToken) =>
        OkEnvelope("Points balance loaded.", await _service.GetPointsAsync(cancellationToken));

    [HttpGet("badges")]
    [Authorize(Policy = PermissionConstants.CitizenOnly)]
    public async Task<IActionResult> Badges(CancellationToken cancellationToken) =>
        OkEnvelope("Badges loaded.", await _service.GetBadgesAsync(cancellationToken));

    [HttpGet("catalog")]
    [Authorize(Policy = PermissionConstants.CitizenOnly)]
    public async Task<IActionResult> Catalog(CancellationToken cancellationToken) =>
        OkEnvelope("Reward catalog loaded.", await _service.GetCatalogAsync(cancellationToken));

    [HttpPost("redeem")]
    [Authorize(Policy = PermissionConstants.CitizenOnly)]
    public async Task<IActionResult> Redeem([FromBody] RedeemRewardRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(request, cancellationToken);
        return OkEnvelope("Reward redeemed.", await _service.RedeemAsync(request, cancellationToken));
    }

    [HttpGet("redemptions")]
    [Authorize(Policy = PermissionConstants.CitizenOnly)]
    public async Task<IActionResult> Redemptions(CancellationToken cancellationToken) =>
        OkEnvelope("Redemption history loaded.", await _service.GetRedemptionsAsync(cancellationToken));

    [HttpGet("history")]
    [Authorize(Policy = PermissionConstants.CitizenOnly)]
    public async Task<IActionResult> History(CancellationToken cancellationToken) =>
        OkEnvelope("Points history loaded.", await _service.GetHistoryAsync(cancellationToken));

    [HttpGet("certificate")]
    [Authorize(Policy = PermissionConstants.CitizenOnly)]
    public async Task<IActionResult> Certificate(CancellationToken cancellationToken)
    {
        var certificate = await _service.GetCertificateAsync(cancellationToken);
        return File(certificate.Content, "text/html; charset=utf-8", certificate.FileName);
    }

    private IActionResult OkEnvelope<T>(string message, T data) => Ok(new { success = true, message, data });

    private async Task ValidateAsync<T>(T request, CancellationToken cancellationToken)
    {
        var validator = _services.GetService<IValidator<T>>();
        if (validator is null) return;
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(result.Errors.Select(error => error.ErrorMessage));
    }
}
