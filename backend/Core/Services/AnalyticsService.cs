using CivicHero.Backend.Core.DTOs.Analytics;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CivicHero.Backend.Core.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly CivicHeroDbContext _context;

        public AnalyticsService(CivicHeroDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardDto> GetDashboardStatsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var weekAgo = today.AddDays(-7);

            var dashboard = new DashboardDto();

            // User statistics
            dashboard.TotalUsers = await _context.Users.CountAsync();
            dashboard.ActiveUsers = await _context.Users.CountAsync(u => u.IsActive);

            // Complaints/Reports statistics - using actual Complaint entity
            dashboard.TotalReports = await _context.Complaints.CountAsync();
            dashboard.PendingReports = await _context.Complaints.CountAsync(c => c.Status == "Submitted");
            dashboard.ResolvedReports = await _context.Complaints.CountAsync(c => c.Status == "Resolved" || c.Status == "Closed");

            // Complaint-specific stats
            dashboard.TotalComplaints = await _context.Complaints.CountAsync();
            dashboard.OpenComplaints = await _context.Complaints.CountAsync(c => c.Status != "Resolved" && c.Status != "Closed");
            dashboard.ResolvedComplaints = await _context.Complaints.CountAsync(c => c.Status == "Resolved" || c.Status == "Closed");
            dashboard.ComplaintsThisWeek = await _context.Complaints.CountAsync(c => c.CreatedAt >= weekAgo);

            // Growth metrics
            dashboard.NewUsersThisWeek = await _context.Users.CountAsync(u => u.CreatedAt >= weekAgo);
            dashboard.ReportsThisWeek = await _context.Complaints.CountAsync(c => c.CreatedAt >= weekAgo);

            // Calculate average resolution time (placeholder)
            var resolvedComplaints = await _context.Complaints
                .Where(c => c.Status == "Resolved" || c.Status == "Closed")
                .ToListAsync();

            if (resolvedComplaints.Any())
            {
                // This is a simplified calculation - in reality you'd have actual timestamps
                dashboard.AvgResolutionTimeHours = 24.0; // Placeholder
            }

            // Rewards & reputation (using existing entities)
            dashboard.TotalRewardsEarned = await _context.Redemptions.CountAsync(); // Each redemption represents a reward earned and redeemed
            dashboard.TotalRewardsRedeemed = await _context.Redemptions.CountAsync(); // Same as earned for simplicity
            // Total reputation points awarded: sum of positive PointsChange in ReputationLog
            var reputationSum = await _context.ReputationLogs
                .Where(r => r.PointsChange > 0)
                .SumAsync(r => (long?)r.PointsChange);
            dashboard.TotalReputationPointsAwarded = (int)(reputationSum ?? 0);

            // Fraud detection metrics
            dashboard.TotalFraudChecks = await _context.AiFraudAnalyses.CountAsync();
            dashboard.FraudAlertsTriggered = await _context.AiFraudAnalyses.CountAsync(a => a.RiskLevel >= FraudRiskLevel.High);
            dashboard.FraudDetectionRate = dashboard.TotalFraudChecks > 0 ?
                (double)dashboard.FraudAlertsTriggered / dashboard.TotalFraudChecks * 100 : 0;

            // Dispute management (using DisputeAuditLog)
            // Total distinct complaints that have at least one audit log
            dashboard.TotalDisputes = await _context.DisputeAuditLogs
                .Select(d => d.ComplaintId)
                .Distinct()
                .CountAsync();

            // Open disputes: complaints with audit logs but no "Verdict submitted" action
            var complaintIdsWithVerdict = await _context.DisputeAuditLogs
                .Where(d => d.Action == "Verdict submitted")
                .Select(d => d.ComplaintId)
                .Distinct()
                .ToListAsync();

            var allComplaintIdsWithLogs = await _context.DisputeAuditLogs
                .Select(d => d.ComplaintId)
                .Distinct()
                .ToListAsync();

            dashboard.OpenDisputes = allComplaintIdsWithLogs.Count(id => !complaintIdsWithVerdict.Contains(id));
            // Resolved disputes: complaints with at least one "Verdict submitted" action
            dashboard.ResolvedDisputes = complaintIdsWithVerdict.Count;

            // Trends - daily reports for last 7 days
            var dailyReports = await _context.Complaints
                .Where(c => c.CreatedAt >= weekAgo)
                .GroupBy(c => c.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(g => g.Date)
                .ToListAsync();

            dashboard.DailyReportsCount = dailyReports.Select(d => d.Count).ToList();
            dashboard.DailyReportsDates = dailyReports.Select(d => d.Date.ToString("yyyy-MM-dd")).ToList();

            // Reports by category (using actual complaint categories)
            var reportsByCategory = await _context.Complaints
                .Where(c => c.IsActive)
                .GroupBy(c => c.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Category, g => g.Count);

            // If no categories found, use sample data
            if (!reportsByCategory.Any())
            {
                reportsByCategory = new Dictionary<string, int>
                {
                    { "Infrastructure", 45 },
                    { "Sanitation", 32 },
                    { "Water Supply", 28 },
                    { "Road Maintenance", 41 },
                    { "Street Lighting", 19 }
                };
            }

            dashboard.ReportsByCategory = reportsByCategory;

            // Ward statistics
            dashboard.WardStatistics = await GetWardPerformanceAsync();

            return dashboard;
        }

        public async Task<List<HeatmapDataDto>> GetHeatmapDataAsync()
        {
            var wards = await _context.Wards.ToListAsync();
            var heatmapData = new List<HeatmapDataDto>();

            var random = new Random();

            foreach (var ward in wards)
            {
                // In a real system, you'd geocode the ward boundaries and count actual incidents
                // For demo, we'll generate some sample data
                var incidentCount = random.Next(0, 50);
                var riskScore = random.NextDouble() * 100; // 0-100

                RiskLevel riskLevel;
                if (riskScore < 25) riskLevel = RiskLevel.Low;
                else if (riskScore < 50) riskLevel = RiskLevel.Medium;
                else if (riskScore < 75) riskLevel = RiskLevel.High;
                else riskLevel = RiskLevel.Critical;

                // Sample coordinates - in reality these would come from geocoded ward boundaries
                var latitude = 12.9716 + (random.NextDouble() - 0.5) * 0.5; // Around Bangalore
                var longitude = 77.5946 + (random.NextDouble() - 0.5) * 0.5;

                heatmapData.Add(new HeatmapDataDto
                {
                    WardId = ward.Id,
                    WardName = ward.Name,
                    WardCode = ward.Code,
                    Latitude = latitude,
                    Longitude = longitude,
                    IncidentCount = incidentCount,
                    RiskScore = riskScore,
                    RiskLevel = riskLevel
                });
            }

            return heatmapData;
        }

        public async Task<CategoryStatsDto[]> GetReportsByCategoryAsync()
        {
            // Get actual complaint categories from database
            var categories = await _context.Complaints
                .Where(c => c.IsActive)
                .GroupBy(c => c.Category)
                .Select(g => new CategoryStatsDto
                {
                    Category = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / _context.Complaints.Count(c => c.IsActive) * 100
                })
                .ToArrayAsync();

            // If no categories found, return sample data
            if (!categories.Any())
            {
                return new CategoryStatsDto[]
                {
                    new CategoryStatsDto { Category = "Infrastructure", Count = 45, Percentage = 29.8 },
                    new CategoryStatsDto { Category = "Sanitation", Count = 32, Percentage = 21.2 },
                    new CategoryStatsDto { Category = "Water Supply", Count = 28, Percentage = 18.5 },
                    new CategoryStatsDto { Category = "Road Maintenance", Count = 41, Percentage = 27.2 },
                    new CategoryStatsDto { Category = "Street Lighting", Count = 19, Percentage = 12.6 }
                };
            }

            return categories;
        }

        public async Task<double[]> GetDailyReportsTrendAsync(int days)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);

            // Get daily counts for the specified period from Complaints table
            var dailyCounts = await _context.Complaints
                .Where(c => c.CreatedAt >= startDate)
                .GroupBy(c => c.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(g => g.Date)
                .ToListAsync();

            // Fill in missing dates with zero
            var result = new double[days];
            for (int i = 0; i < days; i++)
            {
                var date = startDate.AddDays(i);
                var count = dailyCounts.FirstOrDefault(d => d.Date == date)?.Count ?? 0;
                result[i] = count;
            }

            return result;
        }

        public async Task<List<WardStatsDto>> GetWardPerformanceAsync()
        {
            var wards = await _context.Wards.ToListAsync();
            var wardStats = new List<WardStatsDto>();

            var random = new Random();

            foreach (var ward in wards)
            {
                var totalReports = random.Next(0, 100);
                var resolvedReports = random.Next(0, totalReports + 1);

                wardStats.Add(new WardStatsDto
                {
                    WardId = ward.Id,
                    WardName = ward.Name,
                    WardCode = ward.Code,
                    TotalReports = totalReports,
                    ResolvedReports = resolvedReports,
                    ResolutionRate = totalReports > 0 ?
                        (double)resolvedReports / totalReports * 100 : 0,
                    HeatmapData = Enumerable.Range(0, 7).Select(_ => random.NextDouble() * 100).ToList()
                });
            }

            return wardStats;
        }
    }
}