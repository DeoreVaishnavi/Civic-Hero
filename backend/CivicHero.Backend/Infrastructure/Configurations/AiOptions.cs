namespace CivicHero.Backend.Infrastructure.Configurations;

public sealed class AiOptions
{
    public const string SectionName = "AI";

    public string ModelPath { get; set; } = string.Empty;

    public bool EnableLocalModel { get; set; }

    public float Temperature { get; set; }

    public int MaxTokens { get; set; }
}
