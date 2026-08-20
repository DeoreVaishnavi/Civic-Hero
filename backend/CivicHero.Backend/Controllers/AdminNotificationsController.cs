using CivicHero.Backend.Core.Constants;
using CivicHero.Backend.Core.DTOs.Notifications;
using CivicHero.Backend.Core.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/admin/notifications")]
[Authorize(Policy = PermissionConstants.AdminOrAbove)]
public sealed class AdminNotificationsController : ControllerBase
{
    private readonly IAdminNotificationService _service;
    private readonly IServiceProvider _services;

    public AdminNotificationsController(IAdminNotificationService service, IServiceProvider services)
    {
        _service = service;
        _services = services;
    }

    [HttpGet("templates")]
    public async Task<IActionResult> Templates(CancellationToken cancellationToken) =>
        OkEnvelope("Notification templates loaded.", await _service.GetTemplatesAsync(cancellationToken));

    [HttpPost("templates")]
    public async Task<IActionResult> CreateTemplate([FromBody] SaveNotificationTemplateRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(request, cancellationToken);
        return OkEnvelope("Notification template created.", await _service.CreateTemplateAsync(request, cancellationToken));
    }

    [HttpPut("templates/{key}")]
    public async Task<IActionResult> UpdateTemplate(string key, [FromBody] SaveNotificationTemplateRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(request, cancellationToken);
        return OkEnvelope("Notification template updated.", await _service.UpdateTemplateAsync(key, request, cancellationToken));
    }

    [HttpDelete("templates/{key}")]
    public async Task<IActionResult> DeleteTemplate(string key, CancellationToken cancellationToken)
    {
        await _service.DeleteTemplateAsync(key, cancellationToken);
        return OkEnvelope("Notification template deleted.", new { key });
    }

    [HttpPost("broadcast")]
    public async Task<IActionResult> Broadcast([FromBody] AdminBroadcastNotificationRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(request, cancellationToken);
        var result = await _service.BroadcastAsync(request, cancellationToken);
        return OkEnvelope(result.Status == "Pending" ? "Broadcast scheduled." : "Broadcast processed.", result);
    }

    [HttpGet("deliveries")]
    public async Task<IActionResult> Deliveries([FromQuery] NotificationDeliveryQuery query, CancellationToken cancellationToken) =>
        OkEnvelope("Notification delivery history loaded.", await _service.GetDeliveryLogsAsync(query, cancellationToken));

    [HttpGet("deliveries/summary")]
    public async Task<IActionResult> DeliverySummary(CancellationToken cancellationToken) =>
        OkEnvelope("Notification delivery summary loaded.", await _service.GetDeliverySummaryAsync(cancellationToken));

    [HttpGet("deliveries/export")]
    public async Task<IActionResult> ExportDeliveries([FromQuery] NotificationDeliveryQuery query, [FromQuery] string format = "csv", CancellationToken cancellationToken = default)
    {
        var file = await _service.ExportDeliveryLogsAsync(query, format, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpPost("deliveries/{id:long}/retry")]
    public async Task<IActionResult> Retry(long id, [FromBody] RetryNotificationDeliveryRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(request, cancellationToken);
        return OkEnvelope("Notification delivery retry processed.", await _service.RetryDeliveryAsync(id, request, cancellationToken));
    }

    [HttpGet("schedules")]
    public async Task<IActionResult> Schedules(CancellationToken cancellationToken) =>
        OkEnvelope("Scheduled broadcasts loaded.", await _service.GetSchedulesAsync(cancellationToken));

    [HttpDelete("schedules/{id}")]
    public async Task<IActionResult> CancelSchedule(string id, CancellationToken cancellationToken)
    {
        await _service.CancelScheduleAsync(id, cancellationToken);
        return OkEnvelope("Scheduled broadcast cancelled.", new { id });
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
