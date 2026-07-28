using Microsoft.AspNetCore.SignalR;

namespace CivicHero.Backend.Infrastructure.Hubs
{
    public interface IHubProvider
    {
        IHubContext<THub> GetHubContext<THub>() where THub : Hub;
    }

    public class HubProvider : IHubProvider
    {
        private readonly IServiceProvider _serviceProvider;

        public HubProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IHubContext<THub> GetHubContext<THub>() where THub : Hub
        {
            return _serviceProvider.GetRequiredService<IHubContext<THub>>();
        }
    }
}