using CivicHero.Backend.Core.Constants;
using CivicHero.Backend.Core.DTOs.Verification;
using CivicHero.Backend.Core.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CivicHero.Backend.Controllers;
[ApiController,Route("api/v1/verification"),Authorize]
public sealed class VerificationController : ControllerBase
{
 private readonly IVerificationService _service; private readonly IServiceProvider _services;
 public VerificationController(IVerificationService service,IServiceProvider services){_service=service;_services=services;}
 [HttpGet("pending"),Authorize(Policy=PermissionConstants.CitizenOnly)] public async Task<IActionResult> Pending(CancellationToken ct)=>Ok(new{success=true,message="Pending verifications loaded.",data=await _service.GetPendingAsync(ct)});
 [HttpGet("queue"),Authorize(Policy=PermissionConstants.SupervisorOrAbove)] public async Task<IActionResult> Queue([FromQuery]bool overdueOnly,CancellationToken ct)=>Ok(new{success=true,message="Verification queue loaded.",data=await _service.GetSupervisorQueueAsync(overdueOnly,ct)});
 [HttpGet("{complaintId:long}")] public async Task<IActionResult> Get(long complaintId,CancellationToken ct)=>Ok(new{success=true,message="Verification loaded.",data=await _service.GetAsync(complaintId,ct)});
 [HttpPost("{complaintId:long}/geo-check"),Authorize(Policy=PermissionConstants.CitizenOnly)] public async Task<IActionResult> Geo(long complaintId,[FromBody]GeoVerifyRequest request,CancellationToken ct)=>Ok(new{success=true,message="Location checked.",data=await _service.CheckGeoAsync(complaintId,request,ct)});
 [HttpPost("{complaintId:long}/decision"),Authorize(Policy=PermissionConstants.CitizenOnly)] public async Task<IActionResult> Decide(long complaintId,[FromBody]VerifyComplaintRequest request,CancellationToken ct){await Validate(request,ct);return Ok(new{success=true,message=request.Approved?"Resolution approved.":"Resolution rejected and dispute opened.",data=await _service.VerifyAsync(complaintId,request,ct)});}
 [HttpPost("{complaintId:long}/remind"),Authorize(Policy=PermissionConstants.SupervisorOrAbove)] public async Task<IActionResult> Remind(long complaintId,CancellationToken ct)=>Ok(new{success=true,message="Reminder recorded.",data=await _service.RemindAsync(complaintId,ct)});
 private async Task Validate<T>(T request,CancellationToken ct){var v=_services.GetService<IValidator<T>>();if(v is null)return;var r=await v.ValidateAsync(request,ct);if(!r.IsValid)throw new CivicHero.Backend.Core.Exceptions.ValidationException(r.Errors.Select(x=>x.ErrorMessage));}
}
