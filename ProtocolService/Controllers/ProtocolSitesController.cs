using Microsoft.AspNetCore.Mvc;
using Shared.CL;
using Shared.CL.DTOs;
using Shared.CL.Services;
using ProtocolService.DTOs;
using ProtocolService.Services;
using System.Security.Claims;

namespace ProtocolService.Controllers;

// ── Investigators lookup ──────────────────────────────────────────────────────
[ApiController]
[Route("api/investigators")]
public class InvestigatorsController : ControllerBase
{
    private readonly IProtocolSiteService _svc;

    public InvestigatorsController(IProtocolSiteService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<InvestigatorDto>>>> GetAll()
    {
        var list = await _svc.GetInvestigatorsAsync();
        return Ok(ApiResponse<List<InvestigatorDto>>.Ok(list));
    }
}

// ── Protocol-Site assignments ─────────────────────────────────────────────────
[ApiController]
[Route("api/protocols/{protocolId:guid}/sites")]
public class ProtocolSitesController : ControllerBase
{
    private readonly IProtocolSiteService _svc;
    private readonly IAuditClient         _audit;

    public ProtocolSitesController(IProtocolSiteService svc, IAuditClient audit)
    {
        _svc   = svc;
        _audit = audit;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProtocolSiteResponseDto>>> Assign(
        Guid protocolId, [FromBody] AssignSiteDto dto)
    {
        try
        {
            var assigned = await _svc.AssignAsync(protocolId, dto);

            _audit.Log(new AuditLogCreateDto
            {
                ActorUserId = GetUserId(),
                ActorName   = GetUserName(),
                ActorEmail  = GetUserEmail(),
                Action      = "SITE_ASSIGNED_TO_PROTOCOL",
                ServiceName = "ProtocolService",
                Description = $"Site '{assigned.SiteName}' ({assigned.SiteLocation}) assigned to protocol '{assigned.ProtocolTitle}'",
                EntityId    = assigned.ProtocolSiteId.ToString(),
                EntityName  = $"{assigned.SiteName} → {assigned.ProtocolTitle}",
                IpAddress   = GetIp()
            });

            return StatusCode(201, ApiResponse<ProtocolSiteResponseDto>.Ok(assigned, "Site assigned successfully."));
        }
        catch (KeyNotFoundException ex)    { return NotFound(ApiResponse<ProtocolSiteResponseDto>.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return Conflict(ApiResponse<ProtocolSiteResponseDto>.Fail(ex.Message)); }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ProtocolSiteResponseDto>>>> GetByProtocol(Guid protocolId)
    {
        var list = await _svc.GetByProtocolAsync(protocolId);
        return Ok(ApiResponse<List<ProtocolSiteResponseDto>>.Ok(list));
    }

    [HttpPatch("{assignmentId:guid}/status")]
    public async Task<ActionResult<ApiResponse<ProtocolSiteResponseDto>>> UpdateStatus(
        Guid protocolId, Guid assignmentId, [FromBody] UpdateProtocolSiteStatusDto dto)
    {
        try
        {
            await _svc.UpdateStatusAsync(protocolId, assignmentId, dto);

            _audit.Log(new AuditLogCreateDto
            {
                ActorUserId = GetUserId(),
                ActorName   = GetUserName(),
                ActorEmail  = GetUserEmail(),
                Action      = "PROTOCOL_SITE_STATUS_UPDATED",
                ServiceName = "ProtocolService",
                Description = $"Protocol-site assignment {assignmentId} status → '{dto.Status}'",
                EntityId    = assignmentId.ToString(),
                IpAddress   = GetIp()
            });

            return Ok(ApiResponse<ProtocolSiteResponseDto>.OkOnly("Assignment status updated."));
        }
        catch (KeyNotFoundException ex)    { return NotFound(ApiResponse<ProtocolSiteResponseDto>.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return Conflict(ApiResponse<ProtocolSiteResponseDto>.Fail(ex.Message)); }
    }

    [HttpDelete("{assignmentId:guid}")]
    public async Task<ActionResult<ApiResponse<ProtocolSiteResponseDto>>> Remove(
        Guid protocolId, Guid assignmentId)
    {
        try
        {
            await _svc.RemoveAsync(protocolId, assignmentId);

            _audit.Log(new AuditLogCreateDto
            {
                ActorUserId = GetUserId(),
                ActorName   = GetUserName(),
                ActorEmail  = GetUserEmail(),
                Action      = "PROTOCOL_SITE_CLOSED",
                ServiceName = "ProtocolService",
                Description = $"Protocol-site assignment {assignmentId} closed for protocol {protocolId}",
                EntityId    = assignmentId.ToString(),
                IpAddress   = GetIp()
            });

            return Ok(ApiResponse<ProtocolSiteResponseDto>.OkOnly("Assignment closed."));
        }
        catch (KeyNotFoundException ex)    { return NotFound(ApiResponse<ProtocolSiteResponseDto>.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return Conflict(ApiResponse<ProtocolSiteResponseDto>.Fail(ex.Message)); }
    }

    // ── Helpers ──────────────────────────────────────────────
    private Guid?   GetUserId()    { var c = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub"); return c != null && Guid.TryParse(c.Value, out var g) ? g : null; }
    private string  GetUserName()  => User.FindFirst("name")?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
    private string? GetUserEmail() => User.FindFirst("email")?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value;
    private string? GetIp()        => HttpContext.Connection.RemoteIpAddress?.ToString();
}
