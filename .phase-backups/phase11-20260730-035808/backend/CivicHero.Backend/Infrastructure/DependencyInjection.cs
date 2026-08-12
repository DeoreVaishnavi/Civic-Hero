using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Core.Mapping;
using CivicHero.Backend.Core.Services;
using CivicHero.Backend.Core.Validators;
using CivicHero.Backend.Infrastructure.AI;
using CivicHero.Backend.Infrastructure.BackgroundServices;
using CivicHero.Backend.Infrastructure.Configurations;
using CivicHero.Backend.Infrastructure.Data;
using CivicHero.Backend.Infrastructure.HealthChecks;
using CivicHero.Backend.Infrastructure.Repositories;
using CivicHero.Backend.Infrastructure.Security;
using CivicHero.Backend.Infrastructure.Services;
using CivicHero.Backend.Infrastructure.Storage;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<AwsOptions>(configuration.GetSection(AwsOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<SuperAdminBootstrapOptions>(configuration.GetSection(SuperAdminBootstrapOptions.SectionName));
        services.Configure<VerificationOptions>(configuration.GetSection(VerificationOptions.SectionName));
        services.Configure<RewardsOptions>(configuration.GetSection(RewardsOptions.SectionName));
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));

        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("DefaultConnection is not configured.");
        var databaseOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
        services.AddDbContext<CivicDbContext>(options => CivicDbContextFactory.ConfigureMySql(options, connectionString, databaseOptions));

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IComplaintRepository, ComplaintRepository>();
        services.AddScoped<IAssignmentRepository, AssignmentRepository>();
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

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IComplaintService, ComplaintService>();
        services.AddScoped<IAssignmentService, AssignmentService>();
        services.AddScoped<IVerificationService, VerificationService>();
        services.AddScoped<IDisputeService, DisputeService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IRewardService, RewardService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<RuleBasedAiService>();
        services.AddHttpClient<GeminiAiService>();
        services.AddScoped<IAiTriageService, AiTriageService>();
        services.AddScoped<ClassificationService>();
        services.AddScoped<DuplicateDetectionService>();
        services.AddScoped<FraudDetectionService>();
        services.AddScoped<PriorityPredictionService>();
        services.AddScoped<HotspotPredictionService>();
        services.AddScoped<SuperAdminBootstrapper>();
        services.AddHostedService<VerificationTimeoutWorker>();
        services.AddHostedService<RewardAwardWorker>();
        services.AddHostedService<FraudAnalysisWorker>();
        services.AddValidatorsFromAssemblyContaining<RegisterValidator>();
        services.AddAutoMapper(configurationExpression => { }, typeof(AuthMappingProfile).Assembly);
        services.AddCivicHeroJwtAuthentication(configuration);
        services.AddSignalR();

        services.AddSingleton<IAmazonS3>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AwsOptions>>().Value;
            var config = new AmazonS3Config { RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region) };
            return options.HasExplicitCredentials
                ? new AmazonS3Client(new BasicAWSCredentials(options.AccessKey!, options.SecretKey!), config)
                : new AmazonS3Client(config);
        });
        services.AddScoped<IStorageService, AmazonS3StorageService>();
        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database", tags: ["ready", "database"]).AddCheck<S3HealthCheck>("storage", tags: ["ready", "storage"]);
        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider services, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CivicDbContext>();
        if (configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
            await db.Database.MigrateAsync(cancellationToken);
        await scope.ServiceProvider.GetRequiredService<SuperAdminBootstrapper>().SeedAsync(cancellationToken);
    }
}
