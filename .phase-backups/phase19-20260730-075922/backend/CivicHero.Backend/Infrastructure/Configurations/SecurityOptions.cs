namespace CivicHero.Backend.Infrastructure.Configurations;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    public bool EnableRateLimiting { get; set; } = true;
    public bool EnableSecurityHeaders { get; set; } = true;
    public int DefaultRequestBodyLimitMb { get; set; } = 2;
    public int UploadRequestBodyLimitMb { get; set; } = 30;
    public RateLimitPolicyOptions General { get; set; } = new() { PermitLimit = 120, WindowSeconds = 60 };
    public RateLimitPolicyOptions Authentication { get; set; } = new() { PermitLimit = 10, WindowSeconds = 300 };
    public RateLimitPolicyOptions Uploads { get; set; } = new() { PermitLimit = 20, WindowSeconds = 60 };
    public RateLimitPolicyOptions Administration { get; set; } = new() { PermitLimit = 60, WindowSeconds = 60 };
}

public sealed class RateLimitPolicyOptions
{
    public int PermitLimit { get; set; } = 120;
    public int WindowSeconds { get; set; } = 60;
}
