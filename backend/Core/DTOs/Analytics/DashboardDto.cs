using System.Collections.Generic;

namespace CivicHero.Backend.Core.DTOs.Analytics
{
    public class DashboardDto
    {
        // User Statistics
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }

        // Reports Statistics
        public int TotalReports { get; set; }
        public int PendingReports { get; set; }
        public int ResolvedReports { get; set; }

        // Complaints Statistics
        public int TotalComplaints { get; set; }
        public int OpenComplaints { get; set; }
        public int ResolvedComplaints { get; set; }
        public int ComplaintsThisWeek { get; set; }

        // Growth Metrics
        public int NewUsersThisWeek { get; set; }
        public int ReportsThisWeek { get; set; }
        public double AvgResolutionTimeHours { get; set; }

        // Rewards & Reputation
        public int TotalRewardsEarned { get; set; }
        public int TotalRewardsRedeemed { get; set; }
        public int TotalReputationPointsAwarded { get; set; }

        // Fraud Detection
        public int TotalFraudChecks { get; set; }
        public int FraudAlertsTriggered { get; set; }
        public double FraudDetectionRate { get; set; }

        // Dispute Management
        public int TotalDisputes { get; set; }
        public int OpenDisputes { get; set; }
        public int ResolvedDisputes { get; set; }

        // Trends
        public List<int> DailyReportsCount { get; set; } = new List<int>();
        public List<string> DailyReportsDates { get; set; } = new List<string>();
        public Dictionary<string, int> ReportsByCategory { get; set; } = new Dictionary<string, int>();
        public List<WardStatsDto> WardStatistics { get; set; } = new List<WardStatsDto>();
    }
}