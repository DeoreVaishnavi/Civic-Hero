namespace CivicHero.Backend.Core.DTOs.Assignments;

public sealed class AssignmentQuery
{
    private int _page = 1;
    private int _pageSize = 12;

    public int Page { get => _page; set => _page = Math.Max(1, value); }
    public int PageSize { get => _pageSize; set => _pageSize = Math.Clamp(value, 1, 50); }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public long? DepartmentId { get; set; }
    public long? WardId { get; set; }
    public string? Search { get; set; }
    public bool OverdueOnly { get; set; }
}
