namespace CivicHero.Backend.Core.Exceptions;

/// <summary>
/// Represents an exception that is thrown when a business rule
/// defined by the domain is violated.
/// </summary>
public sealed class BusinessRuleViolationException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleViolationException"/> class.
    /// </summary>
    public BusinessRuleViolationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleViolationException"/> class
    /// with the specified error message.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public BusinessRuleViolationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleViolationException"/> class
    /// with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public BusinessRuleViolationException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}
