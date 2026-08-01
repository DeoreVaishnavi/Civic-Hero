namespace CivicHero.Backend.Core.DTOs.Complaints;

public sealed class ComplaintFollowStatusResponse
{
    public long ComplaintId { get; init; }
    public bool IsFollowing { get; init; }
    public int FollowerCount { get; init; }
    public DateTimeOffset? FollowedAt { get; init; }
}

public sealed class FollowedComplaintResponse
{
    public long Id { get; init; }
    public string ReferenceNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = string.Empty;
    public string WardName { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public int UpvoteCount { get; init; }
    public int ImageCount { get; init; }
    public int FollowerCount { get; init; }
    public DateTimeOffset FollowedAt { get; init; }
    public DateTimeOffset LastActivityAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class ComplaintFollowingIdsResponse
{
    public IReadOnlyList<long> ComplaintIds { get; init; } = [];
}
