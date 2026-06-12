using Microsoft.AspNetCore.Mvc;
using PatientService.Services;
using Shared.CL;
using Shared.CL.DTOs;
using Shared.CL.Services;
using Shared.DTOs;
using System.Security.Claims;

namespace PatientService.Controllers;

[ApiController]
[Route("api/visits")]
public class VisitController : ControllerBase
{
    private readonly IVisitService _service;
    private readonly IAuditClient  _audit;

    public VisitController(IVisitService service, IAuditClient audit)
    {
        _service = service;
        _audit   = audit;
    }

    [HttpGet("enrollment/{enrollmentId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> GetByEnrollment(Guid enrollmentId)
    {
        var result = await _service.GetByEnrollmentAsync(enrollmentId);
        return Ok(ApiResponse<object>.Success(result));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<VisitDto>>>> GetFiltered(
        [FromQuery] DateTime? date,
        [FromQuery] Guid? protocolSiteId,
        [FromQuery] string? status)
    {
        var result = await _service.GetFilteredAsync(date, protocolSiteId, status);
        return Ok(ApiResponse<IEnumerable<VisitDto>>.Success(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<VisitDto>>> AddVisit([FromBody] AddVisitDto req)
    {
        var (success, error, data) = await _service.AddAsync(req.EnrollmentId, req.VisitName, req.VisitDate);

        if (!success)
            return NotFound(ApiResponse<VisitDto>.Fail(error!));

        _audit.Log(new AuditLogCreateDto
        {
            ActorUserId = GetUserId(),
            ActorName   = GetUserName(),
            ActorEmail  = GetUserEmail(),
            Action      = "VISIT_SCHEDULED",
            ServiceName = "PatientService",
            Description = $"Visit '{req.VisitName}' scheduled on {req.VisitDate:yyyy-MM-dd} for enrollment {req.EnrollmentId}",
            EntityId    = data!.VisitId.ToString(),
            EntityName  = req.VisitName,
            IpAddress   = GetIp()
        });

        return Ok(ApiResponse<VisitDto>.Success(data!, "Visit added."));
    }

    [HttpPut("{id:guid}/reschedule")]
    public async Task<ActionResult<ApiResponse<VisitDto>>> Reschedule(Guid id, [FromBody] RescheduleVisitDto req)
    {
        var (success, error, data) = await _service.RescheduleAsync(id, req.NewDate);

        if (!success)
            return NotFound(ApiResponse<VisitDto>.Fail(error!));

        _audit.Log(new AuditLogCreateDto
        {
            ActorUserId = GetUserId(),
            ActorName   = GetUserName(),
            ActorEmail  = GetUserEmail(),
            Action      = "VISIT_RESCHEDULED",
            ServiceName = "PatientService",
            Description = $"Visit {id} rescheduled to {req.NewDate:yyyy-MM-dd}",
            EntityId    = id.ToString(),
            IpAddress   = GetIp()
        });

        return Ok(ApiResponse<VisitDto>.Success(data!, "Visit rescheduled."));
    }

    [HttpPut("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<string>>> Cancel(Guid id)
    {
        var (success, error) = await _service.CancelAsync(id);

        if (!success)
            return NotFound(ApiResponse<string>.Fail(error!));

        _audit.Log(new AuditLogCreateDto
        {
            ActorUserId = GetUserId(),
            ActorName   = GetUserName(),
            ActorEmail  = GetUserEmail(),
            Action      = "VISIT_CANCELLED",
            ServiceName = "PatientService",
            Description = $"Visit {id} cancelled",
            EntityId    = id.ToString(),
            IpAddress   = GetIp()
        });

        return Ok(ApiResponse<string>.Success("CANCELLED", "Visit cancelled. Record preserved in DB."));
    }

    [HttpPost("bulk-schedule")]
    public async Task<ActionResult<ApiResponse<string>>> BulkSchedule([FromBody] BulkScheduleDto req)
    {
        var (success, error, count) = await _service.BulkScheduleAsync(req.ProtocolId, req.Visits);

        if (!success)
            return BadRequest(ApiResponse<string>.Fail(error!));

        _audit.Log(new AuditLogCreateDto
        {
            ActorUserId = GetUserId(),
            ActorName   = GetUserName(),
            ActorEmail  = GetUserEmail(),
            Action      = "VISITS_BULK_SCHEDULED",
            ServiceName = "PatientService",
            Description = $"{count} visits bulk-scheduled for protocol {req.ProtocolId}",
            EntityId    = req.ProtocolId.ToString(),
            IpAddress   = GetIp()
        });

        return Ok(ApiResponse<string>.Success($"{count} visit records created successfully."));
    }

    // ── Helpers ──────────────────────────────────────────────
    private Guid?   GetUserId()    { var c = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub"); return c != null && Guid.TryParse(c.Value, out var g) ? g : null; }
    private string  GetUserName()  => User.FindFirst("name")?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
    private string? GetUserEmail() => User.FindFirst("email")?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value;
    private string? GetIp()        => HttpContext.Connection.RemoteIpAddress?.ToString();
}
