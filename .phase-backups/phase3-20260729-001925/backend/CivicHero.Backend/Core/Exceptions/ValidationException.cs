namespace CivicHero.Backend.Core.Exceptions;

public sealed class ValidationException : DomainException
{
    public ValidationException(IEnumerable<string> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors.Where(error => !string.IsNullOrWhiteSpace(error)).Distinct().ToArray();
    }

    public IReadOnlyList<string> Errors { get; }
}
