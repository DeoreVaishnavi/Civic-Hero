namespace CivicHero.Backend.Core.ValueObjects;

/// <summary>
/// Represents reward points earned by a user.
/// </summary>
public sealed class RewardPoints
{
    /// <summary>
    /// Current reward points.
    /// </summary>
    public int Value { get; private set; }

    // Required by Entity Framework Core
    private RewardPoints()
    {
    }

    public RewardPoints(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Reward points cannot be negative.");

        Value = value;
    }

    public RewardPoints Add(int points)
    {
        if (points < 0)
            throw new ArgumentOutOfRangeException(
                nameof(points),
                "Points to add cannot be negative.");

        return new RewardPoints(Value + points);
    }

    public RewardPoints Subtract(int points)
    {
        if (points < 0)
            throw new ArgumentOutOfRangeException(
                nameof(points),
                "Points to subtract cannot be negative.");

        if (points > Value)
            throw new InvalidOperationException(
                "Insufficient reward points.");

        return new RewardPoints(Value - points);
    }

    public override string ToString()
    {
        return Value.ToString();
    }

    public static implicit operator int(RewardPoints rewardPoints)
    {
        return rewardPoints.Value;
    }

    public static explicit operator RewardPoints(int value)
    {
        return new RewardPoints(value);
    }
}