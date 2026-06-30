using CivicHero.Backend.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------
// Add Services
// ---------------------------------------------------------

// Register MVC Controllers
builder.Services.AddControllers();

// Register API Explorer (required for Swagger)
builder.Services.AddEndpointsApiExplorer();

// Register Swagger/OpenAPI
builder.Services.AddSwaggerGen();

// Register Infrastructure Services
builder.Services.AddInfrastructure(builder.Configuration);

// ---------------------------------------------------------
// Build Application
// ---------------------------------------------------------

var app = builder.Build();

// ---------------------------------------------------------
// Configure HTTP Request Pipeline
// ---------------------------------------------------------

// Enable Swagger only in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Redirect HTTP requests to HTTPS
app.UseHttpsRedirection();

// Authentication middleware
// (Will be added in Module 1 - Authentication)
//// app.UseAuthentication();

// Authorization middleware
app.UseAuthorization();

// Map Controller Endpoints
app.MapControllers();

// Run the Application
app.Run();