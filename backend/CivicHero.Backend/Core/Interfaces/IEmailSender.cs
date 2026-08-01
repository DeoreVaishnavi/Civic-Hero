namespace CivicHero.Backend.Core.Interfaces;

public sealed record EmailSendResult(bool Success, string Provider, string? ProviderMessageId, string? Error);

public interface IEmailSender
{
    Task<EmailSendResult> SendAsync(
        string recipientEmail,
        string subject,
        string plainTextBody,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
