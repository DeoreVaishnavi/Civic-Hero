namespace CivicHero.Backend.Infrastructure.Configurations;

public sealed class SuperAdminBootstrapOptions
{
    public const string SectionName = "BootstrapSuperAdmin";

    public bool Enabled { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = "CivicHero Super Admin";
}
