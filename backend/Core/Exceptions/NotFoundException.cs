namespace CivicHero.Backend.Core.Exceptions;

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string message) : base(message) { }
}
