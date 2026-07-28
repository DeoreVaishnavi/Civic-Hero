using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Core.DTOs.FraudDetection;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.AI
{
    public class FraudAnalysisService : IFraudAnalysisService
    {
        private readonly CivicHeroDbContext _context;

        public FraudAnalysisService(CivicHeroDbContext context)
        {
            _context = context;
        }

        public async Task<AiFraudAnalysisDto> AnalyzeComplaintForFraud(int complaintId, string complaintText, double? latitude = null, double? longitude = null, DateTime? incidentTime = null)
        {
            // Simple placeholder implementation
            var analysis = new AiFraudAnalysis
            {
                ComplaintId = complaintId,
                FraudScore = 25, // Low risk placeholder
                RiskLevel = FraudRiskLevel.Low,
                Reasons = "Initial analysis - implementation pending",
                Reviewed = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.AiFraudAnalyses.Add(analysis);
            await _context.SaveChangesAsync();

            return new AiFraudAnalysisDto
            {
                Id = analysis.id,
                ComplaintId = analysis.ComplaintId,
                FraudScore = analysis.FraudScore,
                RiskLevel = analysis.RiskLevel,
                Reasons = analysis.Reasons,
                Reviewed = analysis.Reviewed,
                CreatedAt = analysis.CreatedAt
            };
        }

        public async Task<AiFraudAnalysisDto?> GetFraudAnalysisByComplaintId(int complaintId)
        {
            var analysis = await _context.AiFraudAnalyses
                .FirstOrDefaultAsync(f => f.ComplaintId == complaintId);

            if (analysis == null)
                return null;

            return new AiFraudAnalysisDto
            {
                Id = analysis.id,
                ComplaintId = analysis.ComplaintId,
                FraudScore = analysis.FraudScore,
                RiskLevel = analysis.RiskLevel,
                Reasons = analysis.Reasons,
                Reviewed = analysis.Reviewed,
                CreatedAt = analysis.CreatedAt
            };
        }

        public async Task<bool> MarkAsReviewed(int analysisId)
        {
            var analysis = await _context.AiFraudAnalyses.FindAsync(analysisId);
            if (analysis == null)
                return false;

            analysis.Reviewed = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<double> CalculateAverageFraudScore()
        {
            var average = await _context.AiFraudAnalyses
                .AverageAsync(f => (double?)f.FraudScore);

            return average ?? 0;
        }
    }
}
