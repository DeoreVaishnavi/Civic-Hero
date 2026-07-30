using System.Text.RegularExpressions;
using CivicHero.Backend.Core.Exceptions;

namespace CivicHero.Backend.Infrastructure.Security;

public static partial class PhoneNumberNormalizer
{
    [GeneratedRegex(@"^\+[1-9]\d{7,14}$", RegexOptions.CultureInvariant)]
    private static partial Regex E164Regex();

    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationException(["Phone number is required."]);

        var trimmed = value.Trim();
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        var normalized = trimmed.StartsWith('+') ? $"+{digits}" : digits.Length == 10 ? $"+91{digits}" : $"+{digits}";
        if (!E164Regex().IsMatch(normalized))
            throw new ValidationException(["Enter a valid phone number in international format, for example +919876543210."]);
        return normalized;
    }

    public static string Mask(string normalized)
    {
        if (normalized.Length <= 6) return "******";
        return $"{normalized[..3]}******{normalized[^3..]}";
    }
}
