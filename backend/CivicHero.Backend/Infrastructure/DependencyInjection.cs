using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using CivicHero.Backend.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CivicHero.Backend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<CivicDbContext>(options =>
        {
            options.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString),
                mysqlOptions =>
                {
                    mysqlOptions.CommandTimeout(
                        configuration.GetValue<int>("Database:CommandTimeout"));

                    if (configuration.GetValue<bool>("Database:EnableRetryOnFailure"))
                    {
                        mysqlOptions.EnableRetryOnFailure(
                            maxRetryCount: configuration.GetValue<int>("Database:MaxRetryCount"),
                            maxRetryDelay: TimeSpan.FromSeconds(
                                configuration.GetValue<int>("Database:MaxRetryDelaySeconds")),
                            errorNumbersToAdd: null);
                    }
                });

            options.EnableDetailedErrors(
                configuration.GetValue<bool>("Database:EnableDetailedErrors"));

            options.EnableSensitiveDataLogging(
                configuration.GetValue<bool>("Database:EnableSensitiveDataLogging"));
        });

        services.AddHttpContextAccessor();

        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }
}