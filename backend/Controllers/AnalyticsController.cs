using CivicHero.Backend.Core.DTOs.Analytics;
using CivicHero.Backend.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CivicHero.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        // GET: api/analytics/dashboard
        [HttpGet("dashboard")]
        public async Task<ActionResult<DashboardDto>> GetDashboardStats()
        {
            var stats = await _analyticsService.GetDashboardStatsAsync();
            return Ok(stats);
        }

        // GET: api/analytics/heatmap
        [HttpGet("heatmap")]
        public async Task<ActionResult<List<HeatmapDataDto>>> GetHeatmapData()
        {
            var heatmapData = await _analyticsService.GetHeatmapDataAsync();
            return Ok(heatmapData);
        }

        // GET: api/analytics/categories
        [HttpGet("categories")]
        public async Task<ActionResult<CategoryStatsDto[]>> GetReportsByCategory()
        {
            var categories = await _analyticsService.GetReportsByCategoryAsync();
            return Ok(categories);
        }

        // GET: api/analytics/trend?days=7
        [HttpGet("trend")]
        public async Task<ActionResult<double[]>> GetDailyReportsTrend([FromQuery] int days = 7)
        {
            var trend = await _analyticsService.GetDailyReportsTrendAsync(days);
            return Ok(trend);
        }

        // GET: api/analytics/wards
        [HttpGet("wards")]
        public async Task<ActionResult<List<WardStatsDto>>> GetWardPerformance()
        {
            var wards = await _analyticsService.GetWardPerformanceAsync();
            return Ok(wards);
        }
    }
}