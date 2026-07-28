namespace CivicHero.Backend.Infrastructure.AWS
{
    public interface IAwsNotificationProvider
    {
        Task SendEmailAsync(string? toEmail, string subject, string body);
        Task SendSmsAsync(string? phoneNumber, string message);
    }
}