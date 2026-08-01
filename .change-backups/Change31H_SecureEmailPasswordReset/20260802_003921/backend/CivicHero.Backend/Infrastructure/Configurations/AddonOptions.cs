namespace CivicHero.Backend.Infrastructure.Configurations;

public sealed class AnonymousReportingOptions
{
    public const string SectionName = "AnonymousReporting";
    public bool Enabled { get; set; } = true;
    public int TrackingDays { get; set; } = 30;
    public string SystemReporterEmail { get; set; } = "anonymous-reporter@civichero.invalid";
    public int MaximumSubmissionsPerHour { get; set; } = 3;
}

public sealed class CaptchaOptions
{
    public const string SectionName = "Captcha";
    public bool Enabled { get; set; } = false;
    public string Provider { get; set; } = "GoogleRecaptcha";
    public string? SecretKey { get; set; }
    public string? ExpectedHostname { get; set; }
    public decimal MinimumScore { get; set; } = 0.5m;
    public bool AllowDevelopmentBypass { get; set; } = true;
}

public sealed class SmsOptions
{
    public const string SectionName = "Sms";
    public string Provider { get; set; } = "Development";
    public string? AccountSid { get; set; }
    public string? AuthToken { get; set; }
    public string? FromNumber { get; set; }
    public string? MessagingServiceSid { get; set; }
    public string AppName { get; set; } = "CivicHero";
    public int OtpExpiryMinutes { get; set; } = 5;
    public int ResendCooldownSeconds { get; set; } = 60;
    public int MaximumRequestsPerHour { get; set; } = 5;
}

public sealed class VisualVerificationOptions
{
    public const string SectionName = "VisualVerification";
    public bool Enabled { get; set; } = true;
    public string Provider { get; set; } = "RuleBased";
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gemini-2.5-flash";
    public int WorkerIntervalSeconds { get; set; } = 60;
    public int BatchSize { get; set; } = 10;
    public int MaximumImagesPerGroup { get; set; } = 1;
    public decimal HumanReviewThreshold { get; set; } = 0.70m;
}
