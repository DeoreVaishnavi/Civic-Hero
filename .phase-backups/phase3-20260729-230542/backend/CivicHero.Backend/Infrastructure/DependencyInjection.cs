using Amazon;
using Amazon.Runtime;
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
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(
            configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<AwsOptions>(
            configuration.GetSection(AwsOptions.SectionName));

        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? "Server=YOUR_RDS_ENDPOINT;Port=3306;Database=civicherodb;Uid=admin;Pwd=YOUR_PASSWORD;SslMode=Required;";

        var databaseOptions = configuration
            .GetSection(DatabaseOptions.SectionName)
            .Get<DatabaseOptions>() ?? new DatabaseOptions();

        services.AddDbContext<CivicDbContext>(options =>
            CivicDbContextFactory.ConfigureMySql(options, connectionString, databaseOptions));

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IComplaintRepository, ComplaintRepository>();
        services.AddScoped<IContractorRepository, ContractorRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IRewardRepository, RewardRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWardRepository, WardRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<IDisputeAuditLogRepository, DisputeAuditLogRepository>();
        services.AddScoped<ReputationLogRepository>();
        services.AddScoped<RedemptionRepository>();

        services.AddSingleton<IAmazonS3>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AwsOptions>>().Value;
            var clientConfiguration = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region)
            };

            if (options.HasExplicitCredentials)
            {
                var credentials = new BasicAWSCredentials(options.AccessKey!, options.SecretKey!);
                return new AmazonS3Client(credentials, clientConfiguration);
            }

            return new AmazonS3Client(clientConfiguration);
        });

        services.AddScoped<IStorageService, AmazonS3StorageService>();

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>(
                "database",
                tags: ["ready", "database"])
            .AddCheck<S3HealthCheck>(
                "storage",
                tags: ["ready", "storage"]);

        return services;
    }

    public static async Task InitializeDatabaseAsync(
        this IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CivicDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
