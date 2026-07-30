using CivicHero.Backend.Core.DTOs.AI;
using CivicHero.Backend.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResolutionVerificationController : ControllerBase
    {
        private readonly IResolutionVerificationService _resolutionVerificationService;

        public ResolutionVerificationController(IResolutionVerificationService resolutionVerificationService)
        {
            _resolutionVerificationService = resolutionVerificationService;
        }

        // POST: api/resolutionverification/verify
        // Accepts multipart/form-data with complaint and resolution images
        [HttpPost("verify")]
        public async Task<ActionResult<VisionComparisonResult>> VerifyResolution(
            [FromForm] int complaintId,
            [FromForm] string? complaintDescription,
            [FromForm] double? complaintLatitude,
            [FromForm] double? complaintLongitude,
            [FromForm] string? incidentTime, // ISO string, we'll parse if needed
            [FromForm] IFormFile complaintImage,
            [FromForm] IFormFile resolutionImage)
        {
            if (complaintId <= 0)
                return BadRequest("Invalid complaint ID.");

            if (complaintImage == null || complaintImage.Length == 0)
                return BadRequest("Complaint image is required.");

            if (resolutionImage == null || resolutionImage.Length == 0)
                return BadRequest("Resolution image is required.");

            byte[] complaintImageBytes;
            byte[] resolutionImageBytes;

            try
            {
                using var complaintMs = new MemoryStream();
                await complaintImage.CopyToAsync(complaintMs);
                complaintImageBytes = complaintMs.ToArray();

                using var resolutionMs = new MemoryStream();
                await resolutionImage.CopyToAsync(resolutionMs);
                resolutionImageBytes = resolutionMs.ToArray();
            }
            catch (Exception ex)
            {
                return BadRequest($"Error processing images: {ex.Message}");
            }

            // Parse incidentTime if provided
            DateTime? incidentTimeParsed = null;
            if (!string.IsNullOrWhiteSpace(incidentTime))
            {
                if (DateTime.TryParse(incidentTime, out var parsed))
                {
                    incidentTimeParsed = parsed;
                }
                // If parsing fails, we ignore and continue without incidentTime
            }

            var request = new VisionComparisonRequest
            {
                ComplaintId = complaintId,
                ComplaintImageBytes = complaintImageBytes,
                ResolutionImageBytes = resolutionImageBytes,
                ComplaintDescription = complaintDescription,
                ComplaintLatitude = complaintLatitude,
                ComplaintLongitude = complaintLongitude,
                IncidentTime = incidentTimeParsed
            };

            var result = await _resolutionVerificationService.VerifyResolutionAsync(request);

            return Ok(result);
        }

        // GET: api/complaints/{complaintId}/resolution-verification
        [HttpGet("complaints/{complaintId}/resolution-verification")]
        public async Task<ActionResult<VisionComparisonResult>> GetVerificationByComplaintId(int complaintId)
        {
            // Note: The service method GetVerificationByComplaintId is not yet implemented in the service (throws NotImplementedException)
            // We'll leave it as is for now, but in a real implementation, we would have a repository.
            try
            {
                var result = await _resolutionVerificationService.GetVerificationByComplaintId(complaintId);
                if (result == null)
                    return NotFound();

                return Ok(result);
            }
            catch (NotImplementedException)
            {
                return StatusCode(501, "Not implemented yet.");
            }
        }

        // POST: api/resolutionverification/review/{verificationId}
        [HttpPost("review/{verificationId}")]
        public async Task<ActionResult<bool>> MarkAsReviewed(int verificationId)
        {
            // Note: The service method MarkAsReviewed is not yet implemented in the service (throws NotImplementedException)
            try
            {
                var result = await _resolutionVerificationService.MarkAsReviewed(verificationId);
                if (!result)
                    return NotFound();

                return Ok(result);
            }
            catch (NotImplementedException)
            {
                return StatusCode(501, "Not implemented yet.");
            }
        }
    }
}