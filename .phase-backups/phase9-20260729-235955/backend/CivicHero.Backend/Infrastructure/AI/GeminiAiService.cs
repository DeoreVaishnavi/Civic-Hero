using System.Net.Http.Json;
using System.Text.Json;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Infrastructure.Configurations;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Infrastructure.AI;

public sealed class GeminiAiService : IAiService
{
    private readonly HttpClient _http;
    private readonly AiOptions _options;
    private readonly ILogger<GeminiAiService> _logger;

    public GeminiAiService(HttpClient http, IOptions<AiOptions> options, ILogger<GeminiAiService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
        _http.BaseAddress ??= new Uri("https://generativelanguage.googleapis.com/");
        _http.Timeout = TimeSpan.FromSeconds(20);
    }

    public async Task<AiProviderResult?> AnalyzeAsync(string title, string description, string? currentCategory, CancellationToken cancellationToken = default)
    {
        title ??= string.Empty;
        description ??= string.Empty;
        if (!_options.UseGemini) return null;
        var prompt = $"""
You are an advisory civic complaint triage engine. Return JSON only.
Allowed categories: Pothole, Garbage, Streetlight, Water Leakage, Drainage, Road Damage, Public Safety, Illegal Dumping, Other.
Allowed priorities: Low, Medium, High, Critical.
Fraud verdict: Safe, RiskFlag, HighRisk.
Complaint title: {title}
Complaint description: {description}
Current category: {currentCategory}
Return fields: category, classificationConfidence (0-1), priority, priorityScore (0-1), fraudScore (0-1), fraudVerdict, reasoning.
""";
        var payload = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new { responseMimeType = "application/json", temperature = 0.1, maxOutputTokens = 500 }
        };
        try
        {
            using var response = await _http.PostAsJsonAsync($"v1beta/models/{Uri.EscapeDataString(_options.Model)}:generateContent?key={Uri.EscapeDataString(_options.ApiKey!)}", payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini AI returned status {StatusCode}; CivicHero will use rule-based fallback.", response.StatusCode);
                return null;
            }
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            using var envelope = JsonDocument.Parse(raw);
            var text = envelope.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
            if (string.IsNullOrWhiteSpace(text)) return null;
            using var json = JsonDocument.Parse(text);
            var root = json.RootElement;
            var category = root.TryGetProperty("category", out var c) ? c.GetString() ?? "Other" : "Other";
            var confidence = Decimal(root, "classificationConfidence", 0.70m);
            var priorityText = root.TryGetProperty("priority", out var p) ? p.GetString() : "Medium";
            var priority = Enum.TryParse<ComplaintPriority>(priorityText, true, out var parsed) ? parsed : ComplaintPriority.Medium;
            var priorityScore = Decimal(root, "priorityScore", 0.50m);
            var fraudScore = Decimal(root, "fraudScore", 0.05m);
            var verdict = root.TryGetProperty("fraudVerdict", out var v) ? v.GetString() ?? "Safe" : "Safe";
            var reasoning = root.TryGetProperty("reasoning", out var r) ? r.GetString() ?? "Gemini advisory analysis." : "Gemini advisory analysis.";
            return new AiProviderResult(category, confidence, priority, priorityScore, fraudScore, verdict, reasoning, "Gemini", _options.Model, raw);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            _logger.LogWarning(exception, "Gemini AI failed; CivicHero will use rule-based fallback.");
            return null;
        }
    }

    private static decimal Decimal(JsonElement root, string name, decimal fallback)
    {
        if (!root.TryGetProperty(name, out var value)) return fallback;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number)) return Math.Clamp(number, 0m, 1m);
        return fallback;
    }
}
