namespace CivicHero.Backend.Core.Exceptions;

public sealed class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string message) : base(message) { }
}
