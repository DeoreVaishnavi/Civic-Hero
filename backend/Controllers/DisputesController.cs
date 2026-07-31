using CivicHero.Backend.Core.Constants;
using CivicHero.Backend.Core.DTOs.Disputes;
using CivicHero.Backend.Core.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CivicHero.Backend.Controllers;
[ApiController,Route("api/v1/disputes"),Authorize]
public sealed class DisputesController : ControllerBase
{
 private readonly IDisputeService _service;private readonly IServiceProvider _services;
 public DisputesController(IDisputeService service,IServiceProvider services){_service=service;_services=services;}
 [HttpGet("mine"),Authorize(Policy=PermissionConstants.CitizenOnly)]public async Task<IActionResult> Mine(CancellationToken ct)=>Ok(new{success=true,message="Disputes loaded.",data=await _service.MineAsync(ct)});
 [HttpGet("queue"),Authorize(Policy=PermissionConstants.SupervisorOrAbove)]public async Task<IActionResult> Queue([FromQuery]bool appealsOnly,CancellationToken ct)=>Ok(new{success=true,message="Dispute queue loaded.",data=await _service.QueueAsync(appealsOnly,ct)});
 [HttpGet("{id:long}")]public async Task<IActionResult> Get(long id,CancellationToken ct)=>Ok(new{success=true,message="Dispute loaded.",data=await _service.GetAsync(id,ct)});
 [HttpPost("complaints/{complaintId:long}"),Authorize(Policy=PermissionConstants.CitizenOnly)]public async Task<IActionResult> Raise(long complaintId,[FromBody]RaiseDisputeRequest request,CancellationToken ct){await Validate(request,ct);return Ok(new{success=true,message="Dispute raised.",data=await _service.RaiseAsync(complaintId,request,ct)});}
 [HttpPost("{id:long}/supervisor-decision"),Authorize(Policy=PermissionConstants.SupervisorOrAbove)]public async Task<IActionResult> Supervisor(long id,[FromBody]DisputeDecisionRequest request,CancellationToken ct){await Validate(request,ct);return Ok(new{success=true,message="Supervisor decision saved.",data=await _service.SupervisorDecisionAsync(id,request,ct)});}
 [HttpPost("{id:long}/appeal"),Authorize(Policy=PermissionConstants.CitizenOnly)]public async Task<IActionResult> Appeal(long id,[FromBody]AppealDisputeRequest request,CancellationToken ct){await Validate(request,ct);return Ok(new{success=true,message="Appeal submitted.",data=await _service.AppealAsync(id,request,ct)});}
 [HttpPost("{id:long}/admin-decision"),Authorize(Policy=PermissionConstants.AdminOrAbove)]public async Task<IActionResult> Admin(long id,[FromBody]DisputeDecisionRequest request,CancellationToken ct){await Validate(request,ct);return Ok(new{success=true,message="Admin decision saved.",data=await _service.AdminDecisionAsync(id,request,ct)});}
 private async Task Validate<T>(T request,CancellationToken ct){var v=_services.GetService<IValidator<T>>();if(v is null)return;var r=await v.ValidateAsync(request,ct);if(!r.IsValid)throw new CivicHero.Backend.Core.Exceptions.ValidationException(r.Errors.Select(x=>x.ErrorMessage));}
}
