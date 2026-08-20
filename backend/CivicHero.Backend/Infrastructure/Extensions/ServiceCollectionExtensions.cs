using CivicHero.Backend.Filters;
using CivicHero.Backend.Infrastructure.Security;
using Microsoft.AspNetCore.Http.Features;

namespace CivicHero.Backend.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public const string FrontendCorsPolicy = "CivicHeroFrontend";

    public static IServiceCollection AddCivicHeroServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AuditFilter>();
        services.AddScoped<InputSanitizationFilter>();
        services.AddControllers(options =>
        {
            options.Filters.AddService<InputSanitizationFilter>();
            options.Filters.AddService<AuditFilter>();
        });
        services.AddEndpointsApiExplorer();
        services.AddCivicHeroSwagger();
        services.AddProblemDetails();
        services.AddCivicHeroAuthorization();
        var security = configuration.GetSection(Infrastructure.Configurations.SecurityOptions.SectionName)
            .Get<Infrastructure.Configurations.SecurityOptions>() ?? new Infrastructure.Configurations.SecurityOptions();
        services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = Math.Max(1, security.UploadRequestBodyLimitMb) * 1024L * 1024L;
            options.ValueLengthLimit = 1024 * 1024;
            options.MultipartHeadersLengthLimit = 32 * 1024;
        });

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
