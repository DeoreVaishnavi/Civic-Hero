using CivicHero.Backend.Core.Constants;
using CivicHero.Backend.Core.DTOs.Notifications;
using CivicHero.Backend.Core.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _service;
    private readonly IServiceProvider _services;

    public NotificationsController(INotificationService service, IServiceProvider services)
    {
        _service = service;
        _services = services;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] NotificationQuery query, CancellationToken cancellationToken) =>
        OkEnvelope("Notifications loaded.", await _service.GetAsync(query, cancellationToken));

    [HttpGet("unread")]
    public async Task<IActionResult> Unread(CancellationToken cancellationToken) =>
        OkEnvelope("Unread count loaded.", new { count = await _service.GetUnreadCountAsync(cancellationToken) });

    [HttpPost("{id:long}/read")]
    public async Task<IActionResult> MarkRead(long id, CancellationToken cancellationToken) =>
        OkEnvelope("Notification marked as read.", await _service.MarkReadAsync(id, cancellationToken));

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken) =>
        OkEnvelope("All notifications marked as read.", new { count = await _service.MarkAllReadAsync(cancellationToken) });

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return OkEnvelope("Notification removed.", new { id });
    }

    [HttpGet("preferences")]
    public async Task<IActionResult> Preferences(CancellationToken cancellationToken) =>
        OkEnvelope("Notification preferences loaded.", await _service.GetPreferencesAsync(cancellationToken));

    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdateNotificationPreferencesRequest request, CancellationToken cancellationToken) =>
        OkEnvelope("Notification preferences updated.", await _service.UpdatePreferencesAsync(request, cancellationToken));

    [HttpPost("broadcast")]
    [Authorize(Policy = PermissionConstants.AdminOrAbove)]
    public async Task<IActionResult> Broadcast([FromBody] BroadcastNotificationRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(request, cancellationToken);
        return OkEnvelope("Broadcast queued for active users.", new { recipients = await _service.BroadcastAsync(request, cancellationToken) });
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
