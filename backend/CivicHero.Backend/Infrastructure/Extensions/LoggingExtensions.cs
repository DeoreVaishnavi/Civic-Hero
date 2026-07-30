namespace CivicHero.Backend.Infrastructure.Extensions;

public static class LoggingExtensions
{
    public static ILoggingBuilder AddCivicHeroLogging(this ILoggingBuilder logging)
    {
        logging.ClearProviders();
        logging.AddSimpleConsole(options =>
        {
            options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
            options.SingleLine = true;
            options.IncludeScopes = true;
        });
        logging.AddDebug();

        return logging;
    }
}
