using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.CL;
using Shared.CL.DTOs;
using Shared.CL.Services;
using ProtocolService.Services;
using System.Security.Claims;

namespace ProtocolService.Controllers;

[ApiController]
[Route("api/protocols")]
[Authorize]
public class ProtocolsController : ControllerBase
{
    private readonly IProtocolService _svc;
    private readonly IAuditClient     _audit;

    public ProtocolsController(IProtocolService svc, IAuditClient audit)
    {
        _svc   = svc;
        _audit = audit;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProtocolResponseDto>>> Create([FromBody] CreateProtocolDto dto)
    {
        try
        {
            var created = await _svc.CreateAsync(dto, GetUserId() ?? Guid.Empty);

            _audit.Log(new AuditLogCreateDto
            {
                ActorUserId = GetUserId(),
                ActorName   = GetUserName(),
                ActorEmail  = GetUserEmail(),
                Action      = "PROTOCOL_CREATED",
                ServiceName = "ProtocolService",
                Description = $"Protocol '{created.Title}' created (Phase: {created.Phase})",
                EntityId    = created.ProtocolId.ToString(),
                EntityName  = created.Title,
                IpAddress   = GetIp()
            });

            return StatusCode(201, ApiResponse<ProtocolResponseDto>.Ok(created, "Protocol created successfully."));
        }
        catch (ArgumentException ex)       { return BadRequest(ApiResponse<ProtocolResponseDto>.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return Conflict(ApiResponse<ProtocolResponseDto>.Fail(ex.Message)); }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ProtocolResponseDto>>>> GetAll(
        [FromQuery] string? title,
        [FromQuery] string? phase,
        [FromQuery] string? status)
    {
        var list = await _svc.GetAllAsync(status, phase, title);
        return Ok(ApiResponse<List<ProtocolResponseDto>>.Ok(list));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProtocolResponseDto>>> GetById(Guid id)
    {
        var result = await _svc.GetByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<ProtocolResponseDto>.Fail("Protocol not found."));
        return Ok(ApiResponse<ProtocolResponseDto>.Ok(result));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProtocolResponseDto>>> Update(Guid id, [FromBody] UpdateProtocolDto dto)
    {
        try
        {
            await _svc.UpdateAsync(id, dto);

            _audit.Log(new AuditLogCreateDto
            {
                ActorUserId = GetUserId(),
                ActorName   = GetUserName(),
                ActorEmail  = GetUserEmail(),
                Action      = "PROTOCOL_UPDATED",
                ServiceName = "ProtocolService",
                Description = $"Protocol {id} details updated",
                EntityId    = id.ToString(),
                IpAddress   = GetIp()
            });

            return Ok(ApiResponse<ProtocolResponseDto>.OkOnly("Protocol updated successfully."));
        }
        catch (ArgumentException ex)       { return BadRequest(ApiResponse<ProtocolResponseDto>.Fail(ex.Message)); }
        catch (KeyNotFoundException ex)    { return NotFound(ApiResponse<ProtocolResponseDto>.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return Conflict(ApiResponse<ProtocolResponseDto>.Fail(ex.Message)); }
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<ProtocolResponseDto>>> UpdateStatus(Guid id, [FromBody] UpdateProtocolStatusDto dto)
    {
        try
        {
            await _svc.UpdateStatusAsync(id, dto);

            _audit.Log(new AuditLogCreateDto
            {
                ActorUserId = GetUserId(),
                ActorName   = GetUserName(),
                ActorEmail  = GetUserEmail(),
                Action      = "PROTOCOL_STATUS_CHANGED",
                ServiceName = "ProtocolService",
                Description = $"Protocol {id} status changed to '{dto.Status}'",
                EntityId    = id.ToString(),
                IpAddress   = GetIp()
            });

            return Ok(ApiResponse<ProtocolResponseDto>.OkOnly("Protocol status updated."));
        }
        catch (KeyNotFoundException ex)    { return NotFound(ApiResponse<ProtocolResponseDto>.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return Conflict(ApiResponse<ProtocolResponseDto>.Fail(ex.Message)); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProtocolResponseDto>>> Delete(Guid id)
    {
        try
        {
            await _svc.SoftDeleteAsync(id);

            _audit.Log(new AuditLogCreateDto
            {
                ActorUserId = GetUserId(),
                ActorName   = GetUserName(),
                ActorEmail  = GetUserEmail(),
                Action      = "PROTOCOL_DELETED",
                ServiceName = "ProtocolService",
                Description = $"Protocol {id} soft-deleted",
                EntityId    = id.ToString(),
                IpAddress   = GetIp()
            });

            return Ok(ApiResponse<ProtocolResponseDto>.OkOnly("Protocol deleted successfully."));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<ProtocolResponseDto>.Fail(ex.Message)); }
    }

    // ── Helpers ──────────────────────────────────────────────
    private Guid?   GetUserId()    { var c = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub"); return c != null && Guid.TryParse(c.Value, out var g) ? g : null; }
    private string  GetUserName()  => User.FindFirst("name")?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
    private string? GetUserEmail() => User.FindFirst("email")?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value;
    private string? GetIp()        => HttpContext.Connection.RemoteIpAddress?.ToString();
}
