namespace CivicHero.Backend.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCivicHeroServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddProblemDetails();
        services.AddHealthChecks();

        var configuredOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .GetChildren()
            .Select(section => section.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToArray();

        var allowedOrigins = configuredOrigins.Length > 0
            ? configuredOrigins
            : ["http://localhost:5173"];

        services.AddCors(options =>
        {
            options.AddPolicy("CivicHeroFrontend", policy =>
            {
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }
}
