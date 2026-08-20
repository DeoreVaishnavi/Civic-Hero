namespace CivicHero.Backend.Infrastructure.Configurations;

public sealed class RewardsOptions
{
    public const string SectionName = "Rewards";
    public int ClosedComplaintPoints { get; init; } = 10;
    public int AutoClosedComplaintPoints { get; init; } = 10;
    public int VerificationBonusPoints { get; init; } = 20;
    public int UpvoteBonusPerVote { get; init; } = 2;
    public int AwardWorkerIntervalSeconds { get; init; } = 120;
}
