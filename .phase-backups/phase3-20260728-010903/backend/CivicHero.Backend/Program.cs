using CivicHero.Backend.Infrastructure.Extensions;
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

app.UseCors(ServiceCollectionExtensions.FrontendCorsPolicy);
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/database", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("database")
});
app.MapHealthChecks("/health/storage", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("storage")
});

await app.InitializeDefaultConnectionAsync();
await app.RunAsync();

public partial class Program;

