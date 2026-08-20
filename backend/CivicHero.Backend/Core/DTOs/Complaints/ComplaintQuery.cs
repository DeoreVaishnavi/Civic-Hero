namespace CivicHero.Backend.Core.DTOs.Complaints;

public sealed class ComplaintQuery
{
    private const int MaximumPageSize = 50;
    private int _page = 1;
    private int _pageSize = 12;

    public int Page { get => _page; set => _page = value < 1 ? 1 : value; }
    public int PageSize { get => _pageSize; set => _pageSize = Math.Clamp(value, 1, MaximumPageSize); }
    public string? Status { get; set; }
    public string? Category { get; set; }
    public long? DepartmentId { get; set; }
    public long? WardId { get; set; }
    public string? Search { get; set; }
    public string SortBy { get; set; } = "newest";
    public string SortOrder { get; set; } = "desc";
}
