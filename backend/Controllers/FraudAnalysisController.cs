using CivicHero.Backend.Core.DTOs.FraudDetection;
using CivicHero.Backend.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FraudAnalysisController : ControllerBase
    {
        private readonly IFraudAnalysisService _fraudAnalysisService;

        public FraudAnalysisController(IFraudAnalysisService fraudAnalysisService)
        {
            _fraudAnalysisService = fraudAnalysisService;
        }

        // GET: api/fraudanalysis/{complaintId}
        [HttpGet("{complaintId}")]
        public async Task<ActionResult<AiFraudAnalysisDto>> GetFraudAnalysis(int complaintId)
        {
            var analysis = await _fraudAnalysisService.GetFraudAnalysisByComplaintId(complaintId);
            if (analysis == null)
                return NotFound();

            return Ok(analysis);
        }

        // POST: api/fraudanalysis/analyze
        [HttpPost("analyze")]
        public async Task<ActionResult<AiFraudAnalysisDto>> AnalyzeComplaint([FromBody] FraudAnalysisRequest request)
        {
            if (request == null || request.ComplaintId <= 0)
                return BadRequest("Invalid request data.");

            var analysis = await _fraudAnalysisService.AnalyzeComplaintForFraud(
                request.ComplaintId,
                request.ComplaintText,
                request.Latitude,
                request.Longitude,
                request.IncidentTime);

            return Ok(analysis);
        }

        // POST: api/fraudanalysis/review/{analysisId}
        [HttpPost("review/{analysisId}")]
        public async Task<ActionResult<bool>> MarkAsReviewed(int analysisId)
        {
            var result = await _fraudAnalysisService.MarkAsReviewed(analysisId);
            if (!result)
                return NotFound();

            return Ok(result);
        }

        // GET: api/fraudanalysis/average-score
        [HttpGet("average-score")]
        public async Task<ActionResult<double>> GetAverageFraudScore()
        {
            var averageScore = await _fraudAnalysisService.CalculateAverageFraudScore();
            return Ok(averageScore);
        }
    }

    // Request model for fraud analysis
    public class FraudAnalysisRequest
    {
        public int ComplaintId { get; set; }
        public string ComplaintText { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public DateTime? IncidentTime { get; set; }
    }
}
