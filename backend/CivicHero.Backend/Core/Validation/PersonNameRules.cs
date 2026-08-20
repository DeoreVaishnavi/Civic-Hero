using System.Text.RegularExpressions;

namespace CivicHero.Backend.Core.Validation;

public static partial class PersonNameRules
{
    public const int MinimumLength = 2;
    public const int MaximumLength = 150;

    public static string Normalize(string? value) =>
        string.Join(' ', (value ?? string.Empty)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    public static bool IsValid(string? value)
    {
        var normalized = Normalize(value);
        return normalized.Length is >= MinimumLength and <= MaximumLength
               && AllowedNamePattern().IsMatch(normalized);
    }

    [GeneratedRegex(@"^[\p{L}\p{M}]+(?: [\p{L}\p{M}]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex AllowedNamePattern();
}
