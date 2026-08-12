using System.Text.Json;
using CivicHero.Backend.Infrastructure;
using CivicHero.Backend.Infrastructure.Extensions;
using CivicHeroServiceCollectionExtensions = CivicHero.Backend.Infrastructure.Extensions.ServiceCollectionExtensions;
using CivicHero.Backend.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddCivicHeroLogging();
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

app.UseCors(CivicHeroServiceCollectionExtensions.FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
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
