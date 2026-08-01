using CivicHero.Backend.Core.Constants;
using CivicHero.Backend.Core.DTOs.Rewards;
using CivicHero.Backend.Core.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/admin/rewards")]
[Authorize(Policy = PermissionConstants.AdminOrAbove)]
public sealed class AdminRewardsController : ControllerBase
{
    private readonly IRewardService _service;
    private readonly IServiceProvider _services;

    public AdminRewardsController(IRewardService service, IServiceProvider services)
    {
        _service = service;
        _services = services;
    }

    [HttpGet("catalog")]
    public async Task<IActionResult> Catalog(CancellationToken cancellationToken) =>
        OkEnvelope("Reward catalog loaded.", await _service.GetAdminCatalogAsync(cancellationToken));

    [HttpPost("catalog")]
    public async Task<IActionResult> CreateReward([FromBody] SaveRewardCatalogRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(request, cancellationToken);
        return OkEnvelope("Reward created.", await _service.CreateRewardAsync(request, cancellationToken));
    }

    [HttpPut("catalog/{id:long}")]
    public async Task<IActionResult> UpdateReward(long id, [FromBody] SaveRewardCatalogRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(request, cancellationToken);
        return OkEnvelope("Reward updated.", await _service.UpdateRewardAsync(id, request, cancellationToken));
    }

    [HttpPost("catalog/{id:long}/stock")]
    public async Task<IActionResult> RefillStock(long id, [FromBody] AdjustRewardStockRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(request, cancellationToken);
        return OkEnvelope("Reward stock refilled.", await _service.RefillRewardStockAsync(id, request, cancellationToken));
    }

    [HttpPost("catalog/{id:long}/activate")]
    public async Task<IActionResult> ActivateReward(long id, CancellationToken cancellationToken) =>
        OkEnvelope("Reward activated.", await _service.SetRewardActiveAsync(id, true, cancellationToken));

    [HttpDelete("catalog/{id:long}")]
    public async Task<IActionResult> DeactivateReward(long id, CancellationToken cancellationToken) =>
        OkEnvelope("Reward deactivated.", await _service.SetRewardActiveAsync(id, false, cancellationToken));

    [HttpGet("rules")]
    public async Task<IActionResult> Rules(CancellationToken cancellationToken) =>
        OkEnvelope("Reward rules loaded.", await _service.GetRewardRulesAsync(cancellationToken));

    [HttpPut("rules/badges")]
    public async Task<IActionResult> SaveBadgeRules([FromBody] SaveBadgeRulesRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(request, cancellationToken);
        return OkEnvelope("Badge rules updated.", await _service.SaveBadgeRulesAsync(request, cancellationToken));
    }

    [HttpPut("rules/tiers")]
    public async Task<IActionResult> SaveTierRules([FromBody] SaveTierRulesRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(request, cancellationToken);
        return OkEnvelope("Tier rules updated.", await _service.SaveTierRulesAsync(request, cancellationToken));
    }

    [HttpPost("points/adjust")]
    public async Task<IActionResult> AdjustPoints([FromBody] ManualPointsAdjustmentRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(request, cancellationToken);
        return OkEnvelope("Citizen points adjusted.", await _service.AdjustPointsAsync(request, cancellationToken));
    }

    [HttpGet("redemptions")]
    public async Task<IActionResult> Redemptions([FromQuery] AdminRedemptionQuery query, CancellationToken cancellationToken) =>
        OkEnvelope("Redemptions loaded.", await _service.GetAdminRedemptionsAsync(query, cancellationToken));

    [HttpPut("redemptions/{id:long}/status")]
    public async Task<IActionResult> UpdateRedemptionStatus(long id, [FromBody] UpdateRedemptionStatusRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(request, cancellationToken);
        return OkEnvelope("Redemption status updated.", await _service.UpdateRedemptionStatusAsync(id, request, cancellationToken));
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
