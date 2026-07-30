using System.Text.Json;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CivicHero.Backend.Filters;

public sealed class AuditFilter : IAsyncActionFilter
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AuditFilter> _logger;

    public AuditFilter(IServiceScopeFactory scopeFactory, ICurrentUserService currentUser, ILogger<AuditFilter> logger)
    {
        _scopeFactory = scopeFactory; _currentUser = currentUser; _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var method = context.HttpContext.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method)) { await next(); return; }
        var executed = await next();
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<CivicDbContext>();
            var controller = context.Controller.GetType().Name.Replace("Controller", string.Empty, StringComparison.Ordinal);
            var action = context.ActionDescriptor.RouteValues.TryGetValue("action", out var value) ? value ?? method : method;
            var routeId = context.RouteData.Values.TryGetValue("id", out var id) ? id?.ToString() :
                context.RouteData.Values.TryGetValue("complaintId", out var complaintId) ? complaintId?.ToString() : null;
            var status = executed.HttpContext.Response.StatusCode;
            db.AuditLogs.Add(new AuditLog
            {
                UserId = _currentUser.UserId, UserEmail = _currentUser.Email, UserRole = _currentUser.Role,
                Action = $"{method} {action}", EntityName = controller, EntityId = routeId,
                NewValuesJson = JsonSerializer.Serialize(new { method, endpoint = context.HttpContext.Request.Path.Value, routeId, status }),
                IpAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.HttpContext.Request.Headers.UserAgent.ToString(),
                CorrelationId = context.HttpContext.TraceIdentifier,
                Severity = executed.Exception == null ? "Information" : "Error",
                Success = executed.Exception == null && status < 400, HttpStatusCode = status,
                ErrorMessage = executed.Exception?.Message, CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync(context.HttpContext.RequestAborted);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Unable to persist audit event {Method} {Path}", method, context.HttpContext.Request.Path); }
    }
}
