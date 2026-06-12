using Microsoft.AspNetCore.Mvc;
using PatientService.Services;
using Shared.CL;
using Shared.CL.DTOs;
using Shared.CL.Services;
using Shared.DTOs;
using System.Security.Claims;

namespace PatientService.Controllers;

[ApiController]
[Route("api/patients")]
public class PatientController : ControllerBase
{
    private readonly IPatientService _service;
    private readonly IAuditClient    _audit;

    public PatientController(IPatientService service, IAuditClient audit)
    {
        _service = service;
        _audit   = audit;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<PatientDto>>>> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<PatientDto>>.Success(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PatientDto>>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result is null)
            return NotFound(ApiResponse<PatientDto>.Fail("Patient not found."));
        return Ok(ApiResponse<PatientDto>.Success(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PatientDto>>> Create([FromBody] CreatePatientDto req)
    {
        var (success, error, data) = await _service.CreateAsync(req.Name, req.DateOfBirth, req.ContactInfo);

        if (!success)
            return BadRequest(ApiResponse<PatientDto>.Fail(error!));

        _audit.Log(new AuditLogCreateDto
        {
            ActorUserId = GetUserId(),
            ActorName   = GetUserName(),
            ActorEmail  = GetUserEmail(),
            Action      = "PATIENT_CREATED",
            ServiceName = "PatientService",
            Description = $"Patient '{data!.Name}' registered",
            EntityId    = data.PatientId.ToString(),
            EntityName  = data.Name,
            IpAddress   = GetIp()
        });

        return CreatedAtAction(nameof(GetById),
            new { id = data!.PatientId },
            ApiResponse<PatientDto>.Success(data, "Patient created."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PatientDto>>> Update(Guid id, [FromBody] UpdatePatientDto req)
    {
        var (success, error, data) = await _service.UpdateAsync(id, req.Name, req.DateOfBirth, req.ContactInfo);

        if (!success)
            return error == "Patient not found."
                ? NotFound(ApiResponse<PatientDto>.Fail(error!))
                : BadRequest(ApiResponse<PatientDto>.Fail(error!));

        _audit.Log(new AuditLogCreateDto
        {
            ActorUserId = GetUserId(),
            ActorName   = GetUserName(),
            ActorEmail  = GetUserEmail(),
            Action      = "PATIENT_UPDATED",
            ServiceName = "PatientService",
            Description = $"Patient '{data!.Name}' details updated",
            EntityId    = id.ToString(),
            EntityName  = data.Name,
            IpAddress   = GetIp()
        });

        return Ok(ApiResponse<PatientDto>.Success(data!, "Patient updated."));
    }

    [HttpPut("{id:guid}/deactivate")]
    public async Task<ActionResult<ApiResponse<string>>> Deactivate(Guid id)
    {
        var (success, error) = await _service.DeactivateAsync(id);

        if (!success)
            return error == "Patient not found."
                ? NotFound(ApiResponse<string>.Fail(error!))
                : BadRequest(ApiResponse<string>.Fail(error!));

        _audit.Log(new AuditLogCreateDto
        {
            ActorUserId = GetUserId(),
            ActorName   = GetUserName(),
            ActorEmail  = GetUserEmail(),
            Action      = "PATIENT_DEACTIVATED",
            ServiceName = "PatientService",
            Description = $"Patient {id} deactivated",
            EntityId    = id.ToString(),
            IpAddress   = GetIp()
        });

        return Ok(ApiResponse<string>.Success("INACTIVE", "Patient deactivated."));
    }

    // ── Helpers ──────────────────────────────────────────────
    private Guid?   GetUserId()   { var c = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub"); return c != null && Guid.TryParse(c.Value, out var g) ? g : null; }
    private string  GetUserName() => User.FindFirst("name")?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
    private string? GetUserEmail() => User.FindFirst("email")?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value;
    private string? GetIp()       => HttpContext.Connection.RemoteIpAddress?.ToString();
}
