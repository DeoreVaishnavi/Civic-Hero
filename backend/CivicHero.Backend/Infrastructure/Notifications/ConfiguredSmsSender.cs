using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Configurations;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Infrastructure.Notifications;

public sealed class ConfiguredSmsSender : ISmsSender
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<SmsOptions> _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ConfiguredSmsSender> _logger;

    public ConfiguredSmsSender(HttpClient httpClient, IOptionsMonitor<SmsOptions> options, IHostEnvironment environment, ILogger<ConfiguredSmsSender> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _environment = environment;
        _logger = logger;
    }

    public async Task<SmsSendResult> SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        var options = _options.CurrentValue;
        if (string.Equals(options.Provider, "Development", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("DEVELOPMENT SMS to {Phone}: {Message}", phoneNumber, message);
            return new SmsSendResult(true, "Development", "development-log", null);
        }

        if (!string.Equals(options.Provider, "Twilio", StringComparison.OrdinalIgnoreCase))
            return new SmsSendResult(false, options.Provider, null, "Unsupported SMS provider.");
        if (string.IsNullOrWhiteSpace(options.AccountSid) || string.IsNullOrWhiteSpace(options.AuthToken))
            return new SmsSendResult(false, "Twilio", null, "Twilio credentials are missing.");
        if (string.IsNullOrWhiteSpace(options.FromNumber) && string.IsNullOrWhiteSpace(options.MessagingServiceSid))
            return new SmsSendResult(false, "Twilio", null, "Twilio sender number or Messaging Service SID is missing.");

        var values = new Dictionary<string, string> { ["To"] = phoneNumber, ["Body"] = message };
        if (!string.IsNullOrWhiteSpace(options.MessagingServiceSid)) values["MessagingServiceSid"] = options.MessagingServiceSid;
        else values["From"] = options.FromNumber!;

        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.twilio.com/2010-04-01/Accounts/{Uri.EscapeDataString(options.AccountSid)}/Messages.json")
        {
            Content = new FormUrlEncodedContent(values)
        };
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{options.AccountSid}:{options.AuthToken}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new SmsSendResult(false, "Twilio", null, $"Twilio returned HTTP {(int)response.StatusCode}.");
            using var json = JsonDocument.Parse(raw);
            var sid = json.RootElement.TryGetProperty("sid", out var value) ? value.GetString() : null;
            return new SmsSendResult(true, "Twilio", sid, null);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(exception, "SMS delivery failed.");
            return new SmsSendResult(false, "Twilio", null, "SMS delivery service is unavailable.");
        }
    }
}
