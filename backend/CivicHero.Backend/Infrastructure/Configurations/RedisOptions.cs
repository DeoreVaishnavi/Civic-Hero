
namespace CivicHero.Backend.Infrastructure.Configurations;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";
    public bool Enabled { get; set; }
    public string Configuration { get; set; } = "localhost:6379,abortConnect=false";
    public string InstanceName { get; set; } = "civichero:";
    public int DefaultTtlMinutes { get; set; } = 15;
}
