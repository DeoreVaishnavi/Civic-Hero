using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CivicHero.Backend.Tests.Integration;

public sealed class CivicHeroWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=127.0.0.1;Port=3306;Database=civichero_testing;Uid=test;Pwd=test;SslMode=None;ConnectionTimeout=1;",
                ["Jwt:Issuer"] = "CivicHero.Tests",
                ["Jwt:Audience"] = "CivicHero.TestUsers",
                ["Jwt:SecretKey"] = "CivicHero_Phase14_Test_Key_Only_64_Characters_Minimum_1234567890",
                ["AWS:Region"] = "ap-south-1",
                ["AWS:BucketName"] = "civichero-testing",
                ["BootstrapSuperAdmin:Enabled"] = "false",
                ["Database:ApplyMigrationsOnStartup"] = "false",
                ["Testing:SkipDatabaseInitialization"] = "true",
                ["Testing:DisableBackgroundServices"] = "true",
                ["Security:EnableRateLimiting"] = "false",
                ["Security:EnableSecurityHeaders"] = "true"
            });
        });
    }
}
