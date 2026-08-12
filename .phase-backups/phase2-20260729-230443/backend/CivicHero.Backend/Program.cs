using System.Text.Json;
using CivicHero.Backend.Infrastructure;
using CivicHero.Backend.Infrastructure.Extensions;
using CivicHero.Backend.Infrastructure.Hubs;
using CivicHero.Backend.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddCivicHeroLogging();
var configuration = builder.Configuration;
var jwtSecretKey = configuration["Jwt:SecretKey"];
if (string.IsNullOrWhiteSpace(jwtSecretKey) || jwtSecretKey.Contains("CHANGE_THIS", StringComparison.OrdinalIgnoreCase) || jwtSecretKey.Contains("YOUR_", StringComparison.OrdinalIgnoreCase))
{
    configuration["Jwt:SecretKey"] = "local-development-jwt-secret-key-1234567890";
}

builder.Services.AddCivicHeroServices(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseCivicHeroSwagger();
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors(CivicHero.Backend.Infrastructure.Extensions.ServiceCollectionExtensions.FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = WriteHealthResponseAsync
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponseAsync
});

await app.Services.InitializeDatabaseAsync(app.Configuration);
await app.RunAsync();

static Task WriteHealthResponseAsync(HttpContext context, Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport report)
{
    context.Response.ContentType = "application/json";
    var payload = new
    {
        status = report.Status.ToString(),
        totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
        checks = report.Entries.Select(entry => new
        {
            name = entry.Key,
            status = entry.Value.Status.ToString(),
            description = entry.Value.Description,
            durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 2)
        })
    };

    return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
}

public partial class Program;

