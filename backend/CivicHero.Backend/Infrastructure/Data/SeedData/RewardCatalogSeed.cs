using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Infrastructure.Data.SeedData;

public static class RewardCatalogSeed
{
    private static readonly DateTimeOffset SeedDate = new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

    public static IReadOnlyCollection<RewardCatalog> GetRewards() =>
    [
        Create(1, "Civic Hero Certificate", "Download a personalised civic participation certificate.", 250, RewardType.Certificate, 100000),
        Create(2, "Public Transport Voucher", "A demonstration voucher for recognised civic participation.", 500, RewardType.Voucher, 500),
        Create(3, "Neighborhood Guardian Badge", "A digital badge for citizens who help close multiple civic issues.", 750, RewardType.Badge, 100000),
        Create(4, "CivicHero Eco Kit", "A demonstration eco-friendly merchandise reward.", 1200, RewardType.Merchandise, 100),
        Create(5, "Community Champion Certificate", "Premium certificate for sustained civic contribution.", 2000, RewardType.Certificate, 100000)
    ];

    private static RewardCatalog Create(long id, string name, string description, int cost, RewardType type, int stock) => new()
    {
        Id = id,
        Name = name,
        Description = description,
        PointsCost = cost,
        Type = type,
        StockQuantity = stock,
        IsActive = true,
        IsDeleted = false,
        CreatedAt = SeedDate,
        UpdatedAt = SeedDate
    };
}
