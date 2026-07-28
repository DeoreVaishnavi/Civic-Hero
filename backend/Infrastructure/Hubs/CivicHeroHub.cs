using Microsoft.AspNetCore.SignalR;

namespace CivicHero.Backend.Infrastructure.Hubs
{
    public class CivicHeroHub : Hub
    {
        // You can add methods here for client-to-server calls if needed.
        // For now, server pushes to clients via IHubContext.
    }
}