using CivicHero.Backend.Core.DTOs.Analytics;

namespace CivicHero.Backend.Core.Services;

public interface IAnalyticsService
{
    Task<AnalyticsOverviewResponse> GetOverviewAsync(AnalyticsFilter filter, CancellationToken cancellationToken = default);
    Task<ComplaintAnalyticsResponse> GetComplaintAnalyticsAsync(AnalyticsFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DepartmentAnalyticsRow>> GetDepartmentAnalyticsAsync(AnalyticsFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OfficerAnalyticsRow>> GetOfficerAnalyticsAsync(AnalyticsFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WardAnalyticsRow>> GetWardAnalyticsAsync(AnalyticsFilter filter, CancellationToken cancellationToken = default);
    Task<SlaAnalyticsResponse> GetSlaAnalyticsAsync(AnalyticsFilter filter, CancellationToken cancellationToken = default);
    Task<SatisfactionAnalyticsResponse> GetSatisfactionAnalyticsAsync(AnalyticsFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HeatmapPointResponse>> GetHeatmapAsync(AnalyticsFilter filter, CancellationToken cancellationToken = default);
    Task<AnalyticsExportResult> ExportAsync(string report, AnalyticsFilter filter, CancellationToken cancellationToken = default);
}
