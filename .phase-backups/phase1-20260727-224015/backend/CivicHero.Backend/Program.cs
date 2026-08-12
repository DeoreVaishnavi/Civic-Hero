using CivicHero.Backend.Infrastructure.Extensions;
using CivicHero.Backend.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Register controllers, Swagger, CORS, health checks and other services.
builder.Services.AddCivicHeroServices(builder.Configuration);

var app = builder.Build();

// Custom middleware must run before endpoints.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "CivicHero API v1"
        );

        options.RoutePrefix = "swagger";
    });
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors("CivicHeroFrontend");

app.UseAuthorization();

// Browsers automatically request favicon.ico.
// Return 204 instead of logging an unnecessary 404.
app.MapGet("/favicon.ico", () => Results.NoContent());

app.MapControllers();

app.MapHealthChecks("/health/live");

app.Run();

// Required later for integration testing with WebApplicationFactory.
public partial class Program;