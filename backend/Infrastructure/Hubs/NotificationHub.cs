using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CivicHero.Backend.Infrastructure.Hubs;

[Authorize]
public sealed class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = Context.User?.FindFirstValue(ClaimTypes.Role);
        if (!string.IsNullOrWhiteSpace(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        if (!string.IsNullOrWhiteSpace(role))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"role_{role}");
        await base.OnConnectedAsync();
    }
}
