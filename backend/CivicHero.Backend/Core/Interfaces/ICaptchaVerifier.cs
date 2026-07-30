namespace CivicHero.Backend.Core.Interfaces;

public sealed record CaptchaVerificationResult(bool Success, string Provider, decimal? Score, string? Error);

public interface ICaptchaVerifier
{
    Task<CaptchaVerificationResult> VerifyAsync(string token, string? remoteIp, CancellationToken cancellationToken = default);
}
