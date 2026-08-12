using Microsoft.OpenApi.Models;

namespace CivicHero.Backend.Infrastructure.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddCivicHeroSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "CivicHero API",
                Version = "v1",
                Description = "CivicHero civic complaint governance API"
            });

            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter: Bearer {your JWT access token}",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            };

            options.AddSecurityDefinition("Bearer", securityScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [securityScheme] = Array.Empty<string>()
            });
        });

        return services;
    }

    public static WebApplication UseCivicHeroSwagger(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "CivicHero API v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "CivicHero API Documentation";
            options.DisplayRequestDuration();
        });

        return app;
    }
}
