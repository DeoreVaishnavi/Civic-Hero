using CivicHero.Backend.Infrastructure.Data;
using CivicHero.Backend.Infrastructure.Repositories;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Core.Services;
using CivicHero.Backend.Infrastructure.AI;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<CivicHeroDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    options.UseMySql(
    connectionString,
    new MySqlServerVersion(new Version(8, 0, 36))
);
});

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Repositories
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

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//test
Console.WriteLine("ENVIRONMENT:");
Console.WriteLine(builder.Environment.EnvironmentName);

Console.WriteLine("CONTENT ROOT:");
Console.WriteLine(builder.Environment.ContentRootPath);

Console.WriteLine("CURRENT DIRECTORY:");
Console.WriteLine(Directory.GetCurrentDirectory());

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
