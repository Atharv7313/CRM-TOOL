using AuditLogService.API.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.CL;
using Shared.CL.DTOs;

[AllowAnonymous]
[Route("api/auditlogs")]
[ApiController]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogRepository _repo;

    public AuditLogsController(IAuditLogRepository repo) => _repo = repo;

    // Called by all microservices (fire-and-forget) to record an audit event
    [HttpPost]
    public async Task<ActionResult<ApiResponse<int>>> Create([FromBody] AuditLogCreateDto dto)
    {
        try
        {
            int id = await _repo.CreateLogAsync(dto);
            return id > 0
                ? Ok(ApiResponse<int>.Success(id, "Audit log recorded."))
                : Ok(ApiResponse<int>.Fail("Log could not be saved."));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuditLogsController] {ex.Message}");
            return Ok(ApiResponse<int>.Fail("Audit service error — main request not affected."));
        }
    }

    // All logs — newest first, max 500
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IList<AuditLogListDto>>>> GetAll(
        [FromQuery] int limit = 500)
    {
        var logs = await _repo.GetAllLogsAsync(limit);
        return Ok(ApiResponse<IList<AuditLogListDto>>.Success(logs, $"{logs.Count} record(s)."));
    }

    // Filter by service name e.g. /api/auditlogs/service/PatientService
    [HttpGet("service/{serviceName}")]
    public async Task<ActionResult<ApiResponse<IList<AuditLogListDto>>>> GetByService(string serviceName)
    {
        var logs = await _repo.GetLogsByServiceAsync(serviceName);
        return Ok(ApiResponse<IList<AuditLogListDto>>.Success(logs, $"{logs.Count} record(s)."));
    }

    // Filter by actor user ID
    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<IList<AuditLogListDto>>>> GetByUser(Guid userId)
    {
        var logs = await _repo.GetLogsByUserAsync(userId);
        return Ok(ApiResponse<IList<AuditLogListDto>>.Success(logs, $"{logs.Count} record(s)."));
    }

    // Failed / error events only
    [HttpGet("errors")]
    public async Task<ActionResult<ApiResponse<IList<AuditLogListDto>>>> GetErrors()
    {
        var logs = await _repo.GetErrorLogsAsync();
        return Ok(ApiResponse<IList<AuditLogListDto>>.Success(logs, $"{logs.Count} error(s)."));
    }
}
