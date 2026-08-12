using CivicHero.Backend.Infrastructure.Extensions;
using CivicHero.Backend.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddCivicHeroLogging();
builder.Services.AddCivicHeroServices(builder.Configuration);

var app = builder.Build();

// Required order: correlation first, then exception handling and request logging.
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
app.MapHealthChecks("/health/live");

app.Run();

// Allows WebApplicationFactory<Program> during integration testing.
public partial class Program;
