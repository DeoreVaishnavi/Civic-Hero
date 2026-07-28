using CivicHero.Backend.Core.DTOs.Analytics;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CivicHero.Backend.Core.Interfaces
{
    public interface IAnalyticsService
    {
        Task<DashboardDto> GetDashboardStatsAsync();
        Task<List<HeatmapDataDto>> GetHeatmapDataAsync();
        Task<CategoryStatsDto[]> GetReportsByCategoryAsync();
        Task<double[]> GetDailyReportsTrendAsync(int days);
        Task<List<WardStatsDto>> GetWardPerformanceAsync();
    }
}