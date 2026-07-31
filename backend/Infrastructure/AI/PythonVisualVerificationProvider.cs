using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CivicHero.Backend.Core.DTOs.VisualVerification;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Services;
using CivicHero.Backend.Infrastructure.Configurations;
using CivicHero.Backend.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Infrastructure.AI;

public sealed class PythonVisualVerificationProvider : IVisualVerificationProvider
{
    private readonly HttpClient _httpClient;
    private readonly IStorageService _storage;
    private readonly IOptionsMonitor<VisualVerificationOptions> _options;
    private readonly ILogger<PythonVisualVerificationProvider> _logger;

    public PythonVisualVerificationProvider(HttpClient httpClient, IStorageService storage, IOptionsMonitor<VisualVerificationOptions> options, ILogger<PythonVisualVerificationProvider> logger)
    {
        _httpClient = httpClient;
        _storage = storage;
        _options = options;
        _logger = logger;
    }

    public async Task<VisualProviderResult?> AnalyzeAsync(Complaint complaint, IReadOnlyList<ComplaintImage> beforeImages, IReadOnlyList<ComplaintImage> afterImages, CancellationToken cancellationToken = default)
    {
        if (!_options.CurrentValue.Enabled || !string.Equals(_options.CurrentValue.Provider, "Python", StringComparison.OrdinalIgnoreCase)) return null;
        if (beforeImages.Count == 0 || afterImages.Count == 0) return null;

        var before = await _storage.DownloadAsync(beforeImages[0].S3Key, beforeImages[0].FileName, cancellationToken);
        var after = await _storage.DownloadAsync(afterImages[0].S3Key, afterImages[0].FileName, cancellationToken);
        if (before is null || after is null) return null;
        await using var beforeStream = before.Content;
        await using var afterStream = after.Content;

        try
        {
            using var form = new MultipartFormDataContent
            {
                { new StreamContent(beforeStream) { Headers = { ContentType = new(before.ContentType) } }, "complaint_image", before.FileName },
                { new StreamContent(afterStream) { Headers = { ContentType = new(after.ContentType) } }, "resolution_image", after.FileName },
                { new StringContent(complaint.Id.ToString()), "complaint_id" },
                { new StringContent(complaint.Description), "complaint_description" },
                { new StringContent(complaint.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)), "complaint_latitude" },
                { new StringContent(complaint.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)), "complaint_longitude" }
            };
            using var response = await _httpClient.PostAsync("api/v1/vision/compare-resolution", form, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;
            var payload = await response.Content.ReadFromJsonAsync<Response>(cancellationToken: cancellationToken);
            if (payload is null) return null;
            var verdict = payload.Decision switch
            {
                "LikelyResolved" => "LooksResolved",
                "LikelyNotResolved" => "Suspicious",
                "AnalysisFailed" => "InsufficientEvidence",
                _ => "NeedsHumanReview"
            };
            var confidence = Math.Clamp(payload.ConfidenceScore, 0m, 1m);
            return new VisualProviderResult("Python", "vision-service-v1", verdict,
                verdict == "LooksResolved" ? confidence : 1m - confidence,
                0.75m, payload.IssueStillVisible ? confidence : 1m - confidence, confidence,
                payload.AnalysisDetails, payload.Observations, JsonSerializer.Serialize(payload));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(exception, "Python vision service is unavailable; using visual verification fallback.");
            return null;
        }
    }

    private sealed class Response
    {
        [JsonPropertyName("decision")] public string Decision { get; set; } = string.Empty;
        [JsonPropertyName("confidence_score")] public decimal ConfidenceScore { get; set; }
        [JsonPropertyName("analysis_details")] public string AnalysisDetails { get; set; } = string.Empty;
        [JsonPropertyName("issue_still_visible")] public bool IssueStillVisible { get; set; }
        [JsonPropertyName("observations")] public string[] Observations { get; set; } = [];
    }
}
