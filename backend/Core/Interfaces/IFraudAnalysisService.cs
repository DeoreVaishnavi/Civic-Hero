using CivicHero.Backend.Core.DTOs.FraudDetection;
using System.Threading.Tasks;

namespace CivicHero.Backend.Core.Interfaces
{
    public interface IFraudAnalysisService
    {
        Task<AiFraudAnalysisDto> AnalyzeComplaintForFraud(int complaintId, string complaintText, double? latitude = null, double? longitude = null, DateTime? incidentTime = null);
        Task<AiFraudAnalysisDto?> GetFraudAnalysisByComplaintId(int complaintId);
        Task<bool> MarkAsReviewed(int analysisId);
        Task<double> CalculateAverageFraudScore();
    }
}
