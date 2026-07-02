using System.Text.RegularExpressions;

namespace CivicHero.Backend.Core.ValueObjects;

/// <summary>
/// Represents a validated email address.
/// </summary>
public sealed record EmailAddress
{
    private static readonly Regex EmailRegex =
        new(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    public EmailAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email address cannot be empty.", nameof(value));

        value = value.Trim();

        if (!EmailRegex.IsMatch(value))
            throw new ArgumentException("Invalid email address format.", nameof(value));

        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator string(EmailAddress email)
    {
        return email.Value;
    }

    public static explicit operator EmailAddress(string value)
    {
        return new EmailAddress(value);
    }
}