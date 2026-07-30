using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Infrastructure.AI;

public sealed record AiProviderResult(
    string Category,
    decimal ClassificationConfidence,
    ComplaintPriority Priority,
    decimal PriorityScore,
    decimal FraudScore,
    string FraudVerdict,
    string Reasoning,
    string Provider,
    string Model,
    string? RawResponse = null);
