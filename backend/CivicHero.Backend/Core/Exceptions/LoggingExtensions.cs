using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CivicHero.Backend.Infrastructure.Extensions;

/// <summary>
/// Provides extension methods for configuring Serilog logging.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Configures Serilog for the application host.
    /// </summary>
    public static WebApplicationBuilder AddApplicationLogging(
        this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, configuration) =>
        {
            configuration.ReadFrom.Configuration(context.Configuration);
        });

        return builder;
    }
}