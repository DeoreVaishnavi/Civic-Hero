using System.Text.Json;
using CivicHero.Backend.Infrastructure;
using CivicHero.Backend.Infrastructure.Extensions;
using CivicHero.Backend.Infrastructure.Hubs;
using CivicHero.Backend.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = 32L * 1024L * 1024L;
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(20);
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;

    // The API is exposed only through the internal Docker network in Phase 15.
    // Nginx therefore becomes the trusted edge proxy for forwarded scheme/IP values.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Logging.AddCivicHeroLogging();
builder.Services.AddCivicHeroServices(builder.Configuration);

var app = builder.Build();

app.UseForwardedHeaders();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<RequestSizeGuardMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseCivicHeroSwagger();
}
else
{
    app.UseHsts();
    if (!app.Configuration.GetValue<bool>("Deployment:AllowHttp"))
    {
        app.UseHttpsRedirection();
    }
}

app.UseCors(CivicHero.Backend.Infrastructure.Extensions.ServiceCollectionExtensions.FrontendCorsPolicy);
app.UseAuthentication();
app.UseMiddleware<RateLimitingMiddleware>();
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

if (!app.Configuration.GetValue<bool>("Testing:SkipDatabaseInitialization"))
{
    await app.Services.InitializeDatabaseAsync(app.Configuration);
}

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
