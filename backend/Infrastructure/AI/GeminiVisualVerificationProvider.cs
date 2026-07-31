using System.Net.Http.Json;
using System.Text.Json;
using CivicHero.Backend.Core.DTOs.VisualVerification;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Services;
using CivicHero.Backend.Infrastructure.Configurations;
using CivicHero.Backend.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Infrastructure.AI;

public sealed class GeminiVisualVerificationProvider : IVisualVerificationProvider
{
    private readonly HttpClient _httpClient;
    private readonly IStorageService _storage;
    private readonly IOptionsMonitor<VisualVerificationOptions> _options;
    private readonly ILogger<GeminiVisualVerificationProvider> _logger;

    public GeminiVisualVerificationProvider(HttpClient httpClient, IStorageService storage, IOptionsMonitor<VisualVerificationOptions> options, ILogger<GeminiVisualVerificationProvider> logger)
    {
        _httpClient = httpClient;
        _storage = storage;
        _options = options;
        _logger = logger;
        _httpClient.BaseAddress ??= new Uri("https://generativelanguage.googleapis.com/");
        _httpClient.Timeout = TimeSpan.FromSeconds(45);
    }

    public async Task<VisualProviderResult?> AnalyzeAsync(
        Complaint complaint,
        IReadOnlyList<ComplaintImage> beforeImages,
        IReadOnlyList<ComplaintImage> afterImages,
        CancellationToken cancellationToken = default)
    {
        var options = _options.CurrentValue;
        var supportsConfiguredProvider =
            string.Equals(options.Provider, "Gemini", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(options.Provider, "Python", StringComparison.OrdinalIgnoreCase);
        if (!options.Enabled || !supportsConfiguredProvider || string.IsNullOrWhiteSpace(options.ApiKey))
            return null;
        if (beforeImages.Count == 0 || afterImages.Count == 0) return null;

        try
        {
            var parts = new List<object>
            {
                new { text = $"""
You are an advisory municipal-work visual verification assistant. Compare BEFORE and AFTER evidence for complaint {complaint.Id}: {complaint.Title}.
Do not claim certainty. Detect image quality problems and possible manipulation, but never make the final civic decision.
Return JSON only with: verdict (LooksResolved, NeedsHumanReview, Suspicious, InsufficientEvidence), completionScore 0-1, imageQualityScore 0-1, manipulationRiskScore 0-1, overallConfidence 0-1, reasoning, observations (array of short strings).
""" }
            };

            foreach (var image in beforeImages.Take(Math.Clamp(options.MaximumImagesPerGroup, 1, 3)))
            {
                parts.Add(new { text = "BEFORE IMAGE" });
                parts.Add(await InlinePartAsync(image, cancellationToken));
            }
            foreach (var image in afterImages.Take(Math.Clamp(options.MaximumImagesPerGroup, 1, 3)))
            {
                parts.Add(new { text = "AFTER / RESOLUTION IMAGE" });
                parts.Add(await InlinePartAsync(image, cancellationToken));
            }

            var payload = new
            {
                contents = new[] { new { parts } },
                generationConfig = new { responseMimeType = "application/json", temperature = 0.1, maxOutputTokens = 700 }
            };
            using var response = await _httpClient.PostAsJsonAsync(
                $"v1beta/models/{Uri.EscapeDataString(options.Model)}:generateContent?key={Uri.EscapeDataString(options.ApiKey)}",
                payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini visual verification returned HTTP {StatusCode}.", response.StatusCode);
                return null;
            }

            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            using var envelope = JsonDocument.Parse(raw);
            var text = envelope.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
            if (string.IsNullOrWhiteSpace(text)) return null;
            using var json = JsonDocument.Parse(text);
            var root = json.RootElement;
            var verdict = String(root, "verdict", "NeedsHumanReview");
            var observations = root.TryGetProperty("observations", out var obs) && obs.ValueKind == JsonValueKind.Array
                ? obs.EnumerateArray().Select(item => item.GetString()).Where(item => !string.IsNullOrWhiteSpace(item)).Cast<string>().Take(12).ToArray()
                : Array.Empty<string>();
            return new VisualProviderResult(
                "Gemini", options.Model, verdict,
                Decimal(root, "completionScore", 0.5m),
                Decimal(root, "imageQualityScore", 0.5m),
                Decimal(root, "manipulationRiskScore", 0.5m),
                Decimal(root, "overallConfidence", 0.5m),
                String(root, "reasoning", "Gemini returned an advisory visual assessment."),
                observations, raw);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            _logger.LogWarning(exception, "Gemini visual verification failed; the rule-based human-review fallback will be used.");
            return null;
        }
    }

    private async Task<object> InlinePartAsync(ComplaintImage image, CancellationToken cancellationToken)
    {
        var download = await _storage.DownloadAsync(image.S3Key, image.FileName, cancellationToken)
                       ?? throw new InvalidOperationException($"Evidence image {image.Id} is missing from storage.");
        await using var stream = download.Content;
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        if (memory.Length > 4 * 1024 * 1024)
            throw new InvalidOperationException("Evidence image is too large for safe inline multimodal analysis. Human review is required.");
        return new { inline_data = new { mime_type = image.MimeType, data = Convert.ToBase64String(memory.ToArray()) } };
    }

    private static decimal Decimal(JsonElement root, string name, decimal fallback)
    {
        if (root.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var value))
            return Math.Clamp(value, 0m, 1m);
        return fallback;
    }
    private static string String(JsonElement root, string name, string fallback) =>
        root.TryGetProperty(name, out var element) ? element.GetString() ?? fallback : fallback;
}
