namespace CivicHero.Backend.Core.Interfaces;

public sealed record SmsSendResult(bool Success, string Provider, string? ProviderMessageId, string? Error);

public interface ISmsSender
{
    Task<SmsSendResult> SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}
