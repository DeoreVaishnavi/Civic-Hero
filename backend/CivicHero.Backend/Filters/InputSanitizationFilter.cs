
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CivicHero.Backend.Filters;

public sealed partial class InputSanitizationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (var argument in context.ActionArguments.Values) Sanitize(argument, visited, 0);
    }
    public void OnActionExecuted(ActionExecutedContext context) { }

    private static void Sanitize(object? value, ISet<object> visited, int depth)
    {
        if (value is null || depth > 6 || value is string || value.GetType().IsValueType || !visited.Add(value)) return;
        if (value is IEnumerable sequence)
        {
            foreach (var item in sequence) Sanitize(item, visited, depth + 1);
            return;
        }
        foreach (var property in value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.CanRead && x.CanWrite && x.GetIndexParameters().Length == 0))
        {
            var current = property.GetValue(value);
            if (current is string text)
            {
                var clean = NullCharacter().Replace(text, string.Empty).Trim();
                clean = ScriptBlock().Replace(clean, string.Empty);
                clean = EventAttribute().Replace(clean, string.Empty);
                clean = JavascriptUri().Replace(clean, string.Empty);
                property.SetValue(value, clean);
            }
            else Sanitize(current, visited, depth + 1);
        }
    }

    [GeneratedRegex("\\0")]
    private static partial Regex NullCharacter();
    [GeneratedRegex("<\\s*(script|style|iframe)[^>]*>.*?<\\s*/\\s*\\1\\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ScriptBlock();
    [GeneratedRegex("\\s+on[a-z]+\\s*=\\s*(['\"]).*?\\1", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex EventAttribute();
    [GeneratedRegex("javascript\\s*:", RegexOptions.IgnoreCase)]
    private static partial Regex JavascriptUri();
}
