using System.Net;
using System.Net.Mail;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Configurations;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Infrastructure.Notifications;

public sealed class ConfiguredEmailSender : IEmailSender
{
    private readonly IOptionsMonitor<EmailOptions> _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ConfiguredEmailSender> _logger;

    public ConfiguredEmailSender(
        IOptionsMonitor<EmailOptions> options,
        IHostEnvironment environment,
        ILogger<ConfiguredEmailSender> logger)
    {
        _options = options;
        _environment = environment;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendAsync(
        string recipientEmail,
        string subject,
        string plainTextBody,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        var options = _options.CurrentValue;
        var provider = options.Provider?.Trim() ?? "Development";

        if (provider.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            if (!_environment.IsDevelopment())
                return new EmailSendResult(false, "Development", null, "Development email provider is disabled outside the Development environment.");

            _logger.LogWarning(
                "DEVELOPMENT EMAIL to {Recipient}. Subject: {Subject}. Body: {Body}",
                recipientEmail,
                subject,
                plainTextBody);
            return new EmailSendResult(true, "Development", "development-log", null);
        }

        if (!provider.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
            return new EmailSendResult(false, provider, null, "Unsupported email provider.");

        if (string.IsNullOrWhiteSpace(options.SmtpHost) ||
            string.IsNullOrWhiteSpace(options.FromAddress))
        {
            return new EmailSendResult(false, "Smtp", null, "SMTP host and sender address are required.");
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(options.FromAddress, options.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(new MailAddress(recipientEmail));
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(plainTextBody, null, "text/plain"));

            using var client = new SmtpClient(options.SmtpHost, Math.Clamp(options.SmtpPort, 1, 65535))
            {
                EnableSsl = options.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = options.UseDefaultCredentials
            };

            if (!options.UseDefaultCredentials && !string.IsNullOrWhiteSpace(options.Username))
                client.Credentials = new NetworkCredential(options.Username, options.Password);

            await client.SendMailAsync(message, cancellationToken);
            return new EmailSendResult(true, "Smtp", null, null);
        }
        catch (Exception exception) when (exception is SmtpException or InvalidOperationException or FormatException)
        {
            _logger.LogWarning(exception, "Password-reset email delivery failed for {Recipient}.", recipientEmail);
            return new EmailSendResult(false, "Smtp", null, "Email delivery service is unavailable.");
        }
    }
}
