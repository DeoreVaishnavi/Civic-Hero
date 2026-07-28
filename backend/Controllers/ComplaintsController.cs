using AutoMapper;
using CivicHero.Backend.Core.DTOs.Complaints;
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
    public class ComplaintsController : ControllerBase
    {
        private readonly IComplaintService _complaintService;
        private readonly IMapper _mapper;

        public ComplaintsController(IComplaintService complaintService, IMapper mapper)
        {
            _complaintService = complaintService;
            _mapper = mapper;
        }

        // GET: api/complaints
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ComplaintDto>>> GetAllComplaints()
        {
            var complaints = await _complaintService.GetAllComplaintsAsync();
            return Ok(complaints);
        }

        // GET: api/complaints/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ComplaintDto>> GetComplaint(int id)
        {
            var complaint = await _complaintService.GetComplaintByIdAsync(id);
            if (complaint == null)
                return NotFound();

            return Ok(complaint);
        }

        // GET: api/complaints/user/5
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<ComplaintDto>>> GetComplaintsByUserId(int userId)
        {
            var complaints = await _complaintService.GetComplaintsByUserIdAsync(userId);
            return Ok(complaints);
        }

        // POST: api/complaints
        [HttpPost]
        public async Task<ActionResult<ComplaintDto>> CreateComplaint([FromBody] CreateComplaintDto dto)
        {
            // In a real app, get user ID from claims/token
            var userId = 1; // Placeholder - should be extracted from auth context
            var complaint = await _complaintService.CreateComplaintAsync(userId, dto);
            if (complaint == null)
                return BadRequest("Failed to create complaint");

            return CreatedAtAction(nameof(GetComplaint), new { id = complaint.Id }, complaint);
        }

        // PUT: api/complaints/5
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateComplaint(int id, [FromBody] UpdateComplaintDto dto)
        {
            var complaint = await _complaintService.UpdateComplaintAsync(id, dto);
            if (complaint == null)
                return NotFound();

            return Ok(complaint);
        }

        // DELETE: api/complaints/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteComplaint(int id)
        {
            var result = await _complaintService.DeleteComplaintAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }

        // GET: api/complaints/count
        [HttpGet("count")]
        public async Task<ActionResult<int>> GetComplaintCount()
        {
            var count = await _complaintService.GetComplaintCountAsync();
            return Ok(count);
        }
    }
}