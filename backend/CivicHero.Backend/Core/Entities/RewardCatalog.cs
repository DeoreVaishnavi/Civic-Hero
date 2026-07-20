using CivicHero.Backend.Core.Common;
using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Entities;

/// <summary>
/// Represents a reward available for redemption.
/// Master data maintained by administrators.
/// </summary>
public sealed class RewardCatalog : SoftDeleteEntity
{
    /// <summary>
    /// Reward name.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Detailed description.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Category of reward.
    /// </summary>
    public RewardType Type { get; private set; }

    /// <summary>
    /// Points required to redeem this reward.
    /// </summary>
    public int RequiredPoints { get; private set; }

    /// <summary>
    /// Indicates whether this reward is currently available.
    /// </summary>
    public bool IsActive { get; private set; }

    private RewardCatalog()
    {
        Name = string.Empty;
        Description = string.Empty;
        IsActive = true;
    }

    public RewardCatalog(
        string name,
        string description,
        RewardType type,
        int requiredPoints)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Reward name is required.", nameof(name));

        if (requiredPoints <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(requiredPoints),
                "Required points must be greater than zero.");

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Type = type;
        RequiredPoints = requiredPoints;
        IsActive = true;
    }

    public void Update(
        string name,
        string description,
        RewardType type,
        int requiredPoints)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Reward name is required.", nameof(name));

        if (requiredPoints <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(requiredPoints),
                "Required points must be greater than zero.");

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Type = type;
        RequiredPoints = requiredPoints;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}