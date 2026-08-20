
namespace CivicHero.Backend.Infrastructure.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<T>(string eventName, T payload, string? correlationId = null, CancellationToken cancellationToken = default);
}
