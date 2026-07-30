using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Core.Services;
using CivicHero.Backend.Infrastructure.AI;
using CivicHero.Backend.Infrastructure.BackgroundServices;
using CivicHero.Backend.Infrastructure.Data;
using CivicHero.Backend.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Add database context
builder.Services.AddDbContext<CivicHeroDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Database connection string 'DefaultConnection' not found.");
        }
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    }
);

// Add controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<WardRepository>();
builder.Services.AddScoped<DepartmentRepository>();
builder.Services.AddScoped<IRewardRepository, RewardRepository>();
builder.Services.AddScoped<IReputationService, ReputationService>();
builder.Services.AddScoped<IFraudAnalysisService, FraudAnalysisService>();
builder.Services.AddScoped<IDisputeManagementService, DisputeManagementService>();
builder.Services.AddScoped<IComplaintRepository, ComplaintRepository>();
builder.Services.AddScoped<IComplaintService, ComplaintService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

// Register chat repositories
builder.Services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
builder.Services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
builder.Services.AddScoped<IChatbotService, ChatbotService>();
builder.Services.AddScoped<IChatMessageService, ChatMessageService>();

// Register AI services
builder.Services.AddHttpClient<IAiVisionClient, AiVisionClient>((serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var visionServiceUrl = configuration["VisionService:Url"] ?? "http://localhost:8001";
    client.BaseAddress = new Uri(visionServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30); // 30 second timeout
});

// Register other AI services as singletons or scoped as appropriate
builder.Services.AddSingleton<IMetadataForensics, MetadataForensics>();
builder.Services.AddSingleton<IImageHashService, ImageHashService>();
builder.Services.AddSingleton<FraudScoreCalculator>();
builder.Services.AddSingleton<ResolutionDecisionPolicy>();
builder.Services.AddScoped<IResolutionVerificationService, ResolutionVerificationService>();

// Register Python chatbot client
builder.Services.AddHttpClient<IPythonChatbotClient, PythonChatbotClient>((serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var pythonServiceUrl = configuration["PythonChatbot:BaseUrl"] ?? "http://localhost:8001";
    client.BaseAddress = new Uri(pythonServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(configuration.GetValue("PythonChatbot:TimeoutSeconds", 30));
    // Add the default request header for the internal service key
    var apiKey = configuration["PythonChatbot:InternalServiceKey"];
    if (!string.IsNullOrEmpty(apiKey))
    {
        client.DefaultRequestHeaders.Add("X-Internal-Service-Key", apiKey);
    }
});

// Register background services
builder.Services.AddHostedService<FraudAnalysisWorker>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//test
Console.WriteLine("ENVIRONMENT:");
Console.WriteLine(app.Environment.EnvironmentName);

Console.WriteLine("CONTENT ROOT:");
Console.WriteLine(app.Environment.ContentRootPath);

Console.WriteLine("CURRENT DIRECTORY:");
Console.WriteLine(Directory.GetCurrentDirectory());

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();