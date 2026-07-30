using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Configurations;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Infrastructure.Security;

public sealed class GoogleRecaptchaVerifier : ICaptchaVerifier
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<CaptchaOptions> _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<GoogleRecaptchaVerifier> _logger;

    public GoogleRecaptchaVerifier(HttpClient httpClient, IOptionsMonitor<CaptchaOptions> options, IHostEnvironment environment, ILogger<GoogleRecaptchaVerifier> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _environment = environment;
        _logger = logger;
    }

    public async Task<CaptchaVerificationResult> VerifyAsync(string token, string? remoteIp, CancellationToken cancellationToken = default)
    {
        var options = _options.CurrentValue;
        if (!options.Enabled)
        {
            if (_environment.IsDevelopment() && options.AllowDevelopmentBypass && token == "development-bypass")
                return new CaptchaVerificationResult(true, "DevelopmentBypass", 1m, null);
            return new CaptchaVerificationResult(false, options.Provider, null, "CAPTCHA verification is not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.SecretKey) || string.IsNullOrWhiteSpace(token))
            return new CaptchaVerificationResult(false, options.Provider, null, "CAPTCHA token or secret is missing.");

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["secret"] = options.SecretKey,
            ["response"] = token,
            ["remoteip"] = remoteIp ?? string.Empty
        });

        try
        {
            using var response = await _httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new CaptchaVerificationResult(false, options.Provider, null, $"CAPTCHA provider returned HTTP {(int)response.StatusCode}.");

            var result = await response.Content.ReadFromJsonAsync<RecaptchaResponse>(cancellationToken: cancellationToken);
            if (result is null || !result.Success)
                return new CaptchaVerificationResult(false, options.Provider, result?.Score, string.Join(", ", result?.ErrorCodes ?? []));
            if (result.Score.HasValue && result.Score.Value < options.MinimumScore)
                return new CaptchaVerificationResult(false, options.Provider, result.Score, "CAPTCHA risk score is below the required threshold.");
            if (!string.IsNullOrWhiteSpace(options.ExpectedHostname) && !string.Equals(options.ExpectedHostname, result.Hostname, StringComparison.OrdinalIgnoreCase))
                return new CaptchaVerificationResult(false, options.Provider, result.Score, "CAPTCHA hostname did not match.");
            return new CaptchaVerificationResult(true, options.Provider, result.Score, null);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(exception, "CAPTCHA verification failed.");
            return new CaptchaVerificationResult(false, options.Provider, null, "CAPTCHA verification service is unavailable.");
        }
    }

    private sealed class RecaptchaResponse
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
        [JsonPropertyName("score")] public decimal? Score { get; set; }
        [JsonPropertyName("hostname")] public string? Hostname { get; set; }
        [JsonPropertyName("error-codes")] public string[]? ErrorCodes { get; set; }
    }
}
