using Microsoft.AspNetCore.Mvc;
using PatientService.Services;
using Shared.CL;
using Shared.CL.DTOs;
using Shared.CL.Services;
using Shared.DTOs;
using System.Security.Claims;

namespace PatientService.Controllers;

[ApiController]
[Route("api/enrollments")]
public class EnrollmentController : ControllerBase
{
    private readonly IEnrollmentService _service;
    private readonly IAuditClient       _audit;

    public EnrollmentController(IEnrollmentService service, IAuditClient audit)
    {
        _service = service;
        _audit   = audit;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<EnrollmentDto>>>> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<EnrollmentDto>>.Success(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<EnrollmentDto>>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result is null)
            return NotFound(ApiResponse<EnrollmentDto>.Fail("Enrollment not found."));
        return Ok(ApiResponse<EnrollmentDto>.Success(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<EnrollmentDto>>> Enroll([FromBody] EnrollRequestDto req)
    {
        var (success, error, data) = await _service.EnrollAsync(req.PatientId, req.ProtocolSiteId);

        if (!success)
            return BadRequest(ApiResponse<EnrollmentDto>.Fail(error!));

        _audit.Log(new AuditLogCreateDto
        {
            ActorUserId = GetUserId(),
            ActorName   = GetUserName(),
            ActorEmail  = GetUserEmail(),
            Action      = "PATIENT_ENROLLED",
            ServiceName = "PatientService",
            Description = $"Patient {req.PatientId} enrolled in protocol site {req.ProtocolSiteId}",
            EntityId    = data!.EnrollmentId.ToString(),
            EntityName  = $"Enrollment {data.EnrollmentId}",
            IpAddress   = GetIp()
        });

        return CreatedAtAction(nameof(GetById),
            new { id = data!.EnrollmentId },
            ApiResponse<EnrollmentDto>.Success(data, "Patient enrolled."));
    }

    [HttpPut("{id:guid}/withdraw")]
    public async Task<ActionResult<ApiResponse<string>>> Withdraw(Guid id)
    {
        var (success, error) = await _service.WithdrawAsync(id);

        if (!success)
            return error == "Enrollment not found."
                ? NotFound(ApiResponse<string>.Fail(error!))
                : BadRequest(ApiResponse<string>.Fail(error!));

        _audit.Log(new AuditLogCreateDto
        {
            ActorUserId = GetUserId(),
            ActorName   = GetUserName(),
            ActorEmail  = GetUserEmail(),
            Action      = "PATIENT_WITHDRAWN",
            ServiceName = "PatientService",
            Description = $"Enrollment {id} — patient withdrawn from protocol",
            EntityId    = id.ToString(),
            IpAddress   = GetIp()
        });

        return Ok(ApiResponse<string>.Success("WITHDRAWN", "Patient withdrawn."));
    }

    [HttpGet("protocols")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProtocolDto>>>> GetProtocols()
    {
        var result = await _service.GetProtocolsAsync();
        return Ok(ApiResponse<IEnumerable<ProtocolDto>>.Success(result));
    }

    [HttpGet("protocols/{protocolId:guid}/sites")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProtocolSiteDto>>>> GetSites(Guid protocolId)
    {
        var result = await _service.GetSitesByProtocolAsync(protocolId);
        return Ok(ApiResponse<IEnumerable<ProtocolSiteDto>>.Success(result));
    }

    [HttpGet("protocols/{protocolId:guid}/active-count")]
    public async Task<ActionResult<ApiResponse<int>>> GetActivePatientCount(Guid protocolId)
    {
        var count = await _service.GetActivePatientCountAsync(protocolId);
        return Ok(ApiResponse<int>.Success(count));
    }

    // ── Helpers ──────────────────────────────────────────────
    private Guid?   GetUserId()    { var c = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub"); return c != null && Guid.TryParse(c.Value, out var g) ? g : null; }
    private string  GetUserName()  => User.FindFirst("name")?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
    private string? GetUserEmail() => User.FindFirst("email")?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value;
    private string? GetIp()        => HttpContext.Connection.RemoteIpAddress?.ToString();
}
