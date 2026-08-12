namespace CivicHero.Backend.Core.Common;

public abstract record DomainEvent
{
    public DateTimeOffset OccurredAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
