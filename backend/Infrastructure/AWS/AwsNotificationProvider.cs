using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace CivicHero.Backend.Infrastructure.AWS
{
    public class AwsNotificationProvider : IAwsNotificationProvider
    {
        private readonly IAmazonSimpleEmailService _sesClient;
        private readonly IAmazonSimpleNotificationService _snsClient;
        private readonly ILogger<AwsNotificationProvider> _logger;
        private readonly string _senderEmail; // Configured via environment variable or appsettings

        public AwsNotificationProvider(ILogger<AwsNotificationProvider> logger,
                                       IAmazonSimpleEmailService? sesClient = null,
                                       IAmazonSimpleNotificationService? snsClient = null)
        {
            _logger = logger;
            _sesClient = sesClient ?? new AmazonSimpleEmailServiceClient();
            _snsClient = snsClient ?? new AmazonSimpleNotificationServiceClient();
            // In a real app, you'd read this from configuration
            _senderEmail = Environment.GetEnvironmentVariable("SES_SENDER_EMAIL") ?? "no-reply@civicherobackend.example";
        }

        public async Task SendEmailAsync(string? toEmail, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("Email not sent: recipient email is null or empty.");
                return;
            }

            try
            {
                var sendRequest = new SendEmailRequest
                {
                    Source = _senderEmail,
                    Destination = new Destination
                    {
                        ToAddresses = new List<string> { toEmail }
                    },
                    Message = new Message
                    {
                        Subject = new Content(subject),
                        Body = new Body
                        {
                            Html = new Content { Data = body },
                            Text = new Content { Data = body }
                        }
                    }
                };

                await _sesClient.SendEmailAsync(sendRequest);
                _logger.LogInformation("Email sent to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
                throw; // Optionally rethrow or handle based on policy
            }
        }

        public async Task SendSmsAsync(string? phoneNumber, string message)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                _logger.LogWarning("SMS not sent: phone number is null or empty.");
                return;
            }

            try
            {
                var publishRequest = new PublishRequest
                {
                    PhoneNumber = phoneNumber,
                    Message = message
                };

                await _snsClient.PublishAsync(publishRequest);
                _logger.LogInformation("SMS sent to {PhoneNumber}", phoneNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS to {PhoneNumber}", phoneNumber);
                throw;
            }
        }
    }
}