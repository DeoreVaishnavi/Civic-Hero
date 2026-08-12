using CivicHero.Backend.Filters;
using CivicHero.Backend.Infrastructure.Security;

namespace CivicHero.Backend.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public const string FrontendCorsPolicy = "CivicHeroFrontend";

    public static IServiceCollection AddCivicHeroServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AuditFilter>();
        services.AddControllers(options => options.Filters.AddService<AuditFilter>());
        services.AddEndpointsApiExplorer();
        services.AddCivicHeroSwagger();
        services.AddProblemDetails();
        services.AddCivicHeroAuthorization();

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?.Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (allowedOrigins is null || allowedOrigins.Length == 0) allowedOrigins = ["http://localhost:5173"];

        services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy => policy
            .WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
        services.AddInfrastructure(configuration);
        return services;
    }
}
