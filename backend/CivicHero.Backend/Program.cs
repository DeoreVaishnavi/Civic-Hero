using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using CivicHero.Backend.Infrastructure.Extensions;
using CivicHero.Backend.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ------------------------------------------------------------
    // Logging
    // ------------------------------------------------------------

    builder.AddApplicationLogging();

    // ------------------------------------------------------------
    // Services
    // ------------------------------------------------------------

    builder.Services.AddControllers();

    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddSwaggerDocumentation();

    builder.Services.AddInfrastructure(builder.Configuration);

    // ------------------------------------------------------------
    // Rate Limiting
    // ------------------------------------------------------------

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddFixedWindowLimiter("Default", limiterOptions =>
        {
            limiterOptions.PermitLimit = 100;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 0;
        });
    });

    // ------------------------------------------------------------
    // Build
    // ------------------------------------------------------------

    var app = builder.Build();

    Log.Information("Starting CivicHero API...");

    // ------------------------------------------------------------
    // Swagger
    // ------------------------------------------------------------

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

    // ------------------------------------------------------------
    // Custom Middleware
    // ------------------------------------------------------------

    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseMiddleware<RequestLoggingMiddleware>();

    app.UseMiddleware<GlobalExceptionMiddleware>();

    // ------------------------------------------------------------
    // Built-in Rate Limiter
    // ------------------------------------------------------------

    app.UseRateLimiter();

    // ------------------------------------------------------------
    // Optional Project-Specific Rate Limiting Middleware
    // ------------------------------------------------------------

    app.UseMiddleware<RateLimitingMiddleware>();

    // ------------------------------------------------------------
    // ASP.NET Core Middleware
    // ------------------------------------------------------------

    app.UseHttpsRedirection();

    app.UseAuthorization();

    // ------------------------------------------------------------
    // Endpoints
    // ------------------------------------------------------------

    app.MapControllers();

    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        AllowCachingResponses = false
    });

    Log.Information("CivicHero API started successfully.");

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