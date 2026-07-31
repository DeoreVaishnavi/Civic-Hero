
namespace CivicHero.Backend.Infrastructure.Configurations;

public sealed class AutomationOptions
{
    public const string SectionName = "Automation";
    public bool Enabled { get; set; } = true;
    public int PollIntervalSeconds { get; set; } = 60;
    public int BatchSize { get; set; } = 100;
    public int EscalationGraceHours { get; set; } = 12;
    public int MediumVerificationHours { get; set; } = 72;
    public int HighVerificationHours { get; set; } = 48;
    public int CriticalVerificationHours { get; set; } = 24;
}
