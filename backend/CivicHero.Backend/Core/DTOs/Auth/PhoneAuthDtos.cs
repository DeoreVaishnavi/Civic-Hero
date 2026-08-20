namespace CivicHero.Backend.Core.DTOs.Auth;

public sealed class RequestPhoneLoginOtpRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
}

public sealed class VerifyPhoneLoginOtpRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? TwoFactorCode { get; set; }
}

public sealed class RequestPhoneVerificationOtpRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
}

public sealed class VerifyPhoneNumberRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public sealed class PhoneOtpRequestResponse
{
    public string MaskedPhoneNumber { get; init; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; init; }
    public int ResendAfterSeconds { get; init; }
    public string? DevelopmentCode { get; init; }
}

public sealed class PhoneVerificationStatusResponse
{
    public string? PhoneNumber { get; init; }
    public bool IsPhoneVerified { get; init; }
}
