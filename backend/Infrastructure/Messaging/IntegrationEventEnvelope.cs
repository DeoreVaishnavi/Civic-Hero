
namespace CivicHero.Backend.Infrastructure.Messaging;

public sealed record IntegrationEventEnvelope(
    Guid EventId,
    string EventName,
    DateTimeOffset OccurredAtUtc,
    string CorrelationId,
    object Payload,
    int SchemaVersion = 1);
