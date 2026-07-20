using System.Threading.RateLimiting;
using CivicHero.Backend.Infrastructure.Extensions;
using CivicHero.Backend.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ============================================================
    // Logging
    // ============================================================

    builder.AddApplicationLogging();

    // ============================================================
    // Core Services
    // ============================================================

    builder.Services.AddControllers();

    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddSwaggerDocumentation();

    builder.Services.AddHttpContextAccessor();

    builder.Services.AddHealthChecks();

    // ============================================================
    // CORS
    // ============================================================

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Default", policy =>
        {
            policy
                .WithOrigins(
                    builder.Configuration
                        .GetSection("Cors:AllowedOrigins")
                        .Get<string[]>() ?? Array.Empty<string>())
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    // ============================================================
    // Infrastructure
    // ============================================================

    builder.Services.AddInfrastructure(builder.Configuration);

    // ============================================================
    // Rate Limiting
    // ============================================================

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddFixedWindowLimiter("Default", limiterOptions =>
        {
            limiterOptions.PermitLimit = 100;

            limiterOptions.Window = TimeSpan.FromMinutes(1);

            limiterOptions.QueueProcessingOrder =
                QueueProcessingOrder.OldestFirst;

            limiterOptions.QueueLimit = 0;
        });
    });

    // ============================================================
    // Build
    // ============================================================

    var app = builder.Build();

    Log.Information("========================================");
    Log.Information("Starting CivicHero API...");
    Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);
    Log.Information("========================================");

    // ============================================================
    // Swagger
    // ============================================================

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();

        app.UseSwaggerUI(options =>
        {
            options.DocumentTitle = "CivicHero API";

            options.SwaggerEndpoint(
                "/swagger/v1/swagger.json",
                "CivicHero API v1");

            options.RoutePrefix = "swagger";
        });
    }
    else
    {
        app.UseHsts();
    }

    // ============================================================
    // Custom Middleware
    // ============================================================

    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseMiddleware<RequestLoggingMiddleware>();

    app.UseMiddleware<GlobalExceptionMiddleware>();

    // ============================================================
    // Built-in Rate Limiter
    // ============================================================

    app.UseRateLimiter();

    // ============================================================
    // Custom Rate Limiter
    // ============================================================

    app.UseMiddleware<RateLimitingMiddleware>();

    // ============================================================
    // ASP.NET Core Middleware
    // ============================================================

    app.UseHttpsRedirection();

    app.UseCors("Default");

    // ------------------------------------------------------------
    // Authentication (Coming in JWT Module)
    // ------------------------------------------------------------
    // app.UseAuthentication();

    app.UseAuthorization();

    // ============================================================
    // Endpoints
    // ============================================================

    app.MapControllers();

    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        AllowCachingResponses = false
    });

    // ============================================================
    // Startup Completed
    // ============================================================

    Log.Information("========================================");
    Log.Information("CivicHero API started successfully.");
    Log.Information("Swagger : /swagger");
    Log.Information("Health  : /health");
    Log.Information("========================================");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}