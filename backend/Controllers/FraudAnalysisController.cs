using CivicHero.Backend.Core.DTOs.FraudDetection;
using CivicHero.Backend.Core.Interfaces;
using Microsoft.AspNetCore.Http;
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
        // Accepts multipart/form-data with optional image file
        [HttpPost("analyze")]
        public async Task<ActionResult<AiFraudAnalysisDto>> AnalyzeComplaint(
            [FromForm] int complaintId,
            [FromForm] string complaintText,
            [FromForm] double? latitude,
            [FromForm] double? longitude,
            [FromForm] DateTime? incidentTime,
            [FromForm] IFormFile? imageFile)
        {
            if (complaintId <= 0)
                return BadRequest("Invalid complaint ID.");

            byte[]? imageBytes = null;
            if (imageFile != null && imageFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await imageFile.CopyToAsync(ms);
                imageBytes = ms.ToArray();
            }

            var analysis = await _fraudAnalysisService.AnalyzeComplaintForFraud(
                complaintId,
                complaintText,
                imageBytes,
                latitude,
                longitude,
                incidentTime);

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
}