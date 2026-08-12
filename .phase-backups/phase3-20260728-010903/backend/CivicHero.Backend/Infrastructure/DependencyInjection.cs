using Amazon;
using Amazon.S3;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Configurations;
using CivicHero.Backend.Infrastructure.Data;
using CivicHero.Backend.Infrastructure.HealthChecks;
using CivicHero.Backend.Infrastructure.Repositories;
using CivicHero.Backend.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Infrastructure;

public static class DependencyInjection
{
    private const string FallbackConnectionString =
        "Server=127.0.0.1;Port=3306;Database=civichero_not_configured;" +
        "User=civichero;Password=not-configured;SslMode=None;Connection Timeout=3;";

    public static IServiceCollection AddCivicHeroInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName));
        services.AddOptions<AwsOptions>()
            .Bind(configuration.GetSection(AwsOptions.SectionName));

        services.AddDbContext<CivicDbContext>((serviceProvider, options) =>
        {
            var databaseOptions = serviceProvider
                .GetRequiredService<IOptions<DatabaseOptions>>()
                .Value;
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            options.UseMySql(
                string.IsNullOrWhiteSpace(connectionString)
                    ? FallbackConnectionString
                    : connectionString,
                ServerVersion.Parse($"{databaseOptions.MySqlVersion}-mysql"),
                mySql =>
                {
                    mySql.MigrationsAssembly(typeof(CivicDbContext).Assembly.FullName);
                    mySql.CommandTimeout(databaseOptions.CommandTimeoutSeconds);
                    mySql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                });
        });

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton<IAmazonS3>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AwsOptions>>().Value;
            var s3Configuration = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region),
                ForcePathStyle = options.ForcePathStyle
            };

            if (!string.IsNullOrWhiteSpace(options.S3ServiceUrl))
            {
                s3Configuration.ServiceURL = options.S3ServiceUrl;
                s3Configuration.RegionEndpoint = null;
            }

            return new AmazonS3Client(s3Configuration);
        });
        services.AddScoped<IStorageService, AmazonS3StorageService>();

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>(
                "aws-rds-mysql",
                tags: ["database", "ready"])
            .AddCheck<S3HealthCheck>(
                "amazon-s3",
                tags: ["storage", "ready"]);

        return services;
    }
}

