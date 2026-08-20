using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Infrastructure.AI;

public sealed class RuleBasedAiService : IAiService
{
    private static readonly Dictionary<string, string[]> CategoryKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Pothole"] = ["pothole", "hole in road", "road pit", "खड्डा", "खड्डे"],
        ["Garbage"] = ["garbage", "trash", "waste", "rubbish", "कचरा"],
        ["Streetlight"] = ["streetlight", "street light", "lamp", "dark road", "दिवा"],
        ["Water Leakage"] = ["water leak", "pipeline", "pipe burst", "leakage", "पाणी गळती"],
        ["Drainage"] = ["drain", "sewage", "gutter", "overflow", "नाला"],
        ["Road Damage"] = ["road damage", "broken road", "cracked road", "damaged road"],
        ["Public Safety"] = ["danger", "unsafe", "accident", "electrical wire", "collapse", "fire"],
        ["Illegal Dumping"] = ["illegal dumping", "debris", "construction waste", "dumped"],
    };

    public Task<AiProviderResult?> AnalyzeAsync(string title, string description, string? currentCategory, CancellationToken cancellationToken = default)
    {
        title ??= string.Empty;
        description ??= string.Empty;
        var text = $"{title} {description}".ToLowerInvariant();
        var scores = CategoryKeywords.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Sum(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase) ? 1 : 0),
            StringComparer.OrdinalIgnoreCase);
        var best = scores.OrderByDescending(pair => pair.Value).FirstOrDefault();
        var category = best.Value > 0 ? best.Key : NormalizeCategory(currentCategory);
        var confidence = best.Value switch { >= 3 => 0.94m, 2 => 0.86m, 1 => 0.72m, _ => 0.45m };

        var prioritySignals = new List<string>();
        var priority = ComplaintPriority.Medium;
        var priorityScore = 0.50m;
        if (ContainsAny(text, "death", "fire", "live wire", "collapse", "major accident", "life threatening", "explosion"))
        { priority = ComplaintPriority.Critical; priorityScore = 0.96m; prioritySignals.Add("Immediate danger or life-safety keyword detected."); }
        else if (ContainsAny(text, "accident", "dangerous", "sewage overflow", "pipe burst", "blocked road", "school", "hospital"))
        { priority = ComplaintPriority.High; priorityScore = 0.82m; prioritySignals.Add("High-impact public safety or essential-service keyword detected."); }
        else if (ContainsAny(text, "minor", "small", "single lamp", "not urgent"))
        { priority = ComplaintPriority.Low; priorityScore = 0.68m; prioritySignals.Add("Low-severity wording detected."); }
        else prioritySignals.Add("Standard municipal service priority applied.");

        var fraudSignals = new List<string>();
        decimal fraud = 0.05m;
        if (title.Length < 5 || description.Length < 15) { fraud += 0.18m; fraudSignals.Add("Very limited complaint detail."); }
        if (ContainsAny(text, "http://", "https://", "buy now", "click here", "free money")) { fraud += 0.45m; fraudSignals.Add("Spam or promotional pattern."); }
        if (RepeatedRatio(text) > 0.55m) { fraud += 0.28m; fraudSignals.Add("High repeated-token ratio."); }
        fraud = Math.Min(1m, fraud);
        var verdict = fraud >= 0.75m ? "HighRisk" : fraud >= 0.45m ? "RiskFlag" : "Safe";
        var reasoning = $"Category matched as {category}. {string.Join(" ", prioritySignals)} {string.Join(" ", fraudSignals)}".Trim();
        return Task.FromResult<AiProviderResult?>(new(category, confidence, priority, priorityScore, fraud, verdict, reasoning, "RuleBased", "civichero-rules-v1"));
    }

    private static string NormalizeCategory(string? value) => string.IsNullOrWhiteSpace(value) ? "Other" : value.Trim();
    private static bool ContainsAny(string text, params string[] values) => values.Any(value => text.Contains(value, StringComparison.OrdinalIgnoreCase));
    private static decimal RepeatedRatio(string text)
    {
        var tokens = Tokenize(text);
        if (tokens.Length == 0) return 0;
        return 1m - ((decimal)tokens.Distinct(StringComparer.OrdinalIgnoreCase).Count() / tokens.Length);
    }
    internal static string[] Tokenize(string text) => text.ToLowerInvariant().Split([' ', '\t', '\r', '\n', '.', ',', ';', ':', '!', '?', '-', '_', '/', '\\', '(', ')'], StringSplitOptions.RemoveEmptyEntries)
        .Where(token => token.Length > 2).ToArray();
}
