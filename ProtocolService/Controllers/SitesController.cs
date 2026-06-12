using Microsoft.AspNetCore.Mvc;
using Shared.CL;
using Shared.CL.DTOs;
using Shared.CL.Services;
using ProtocolService.Services;
using System.Security.Claims;

namespace ProtocolService.Controllers;

[ApiController]
[Route("api/sites")]
public class SitesController : ControllerBase
{
    private readonly ISiteService         _svc;
    private readonly IProtocolSiteService _protocolSiteSvc;
    private readonly IAuditClient         _audit;

    public SitesController(ISiteService svc, IProtocolSiteService protocolSiteSvc, IAuditClient audit)
    {
        _svc             = svc;
        _protocolSiteSvc = protocolSiteSvc;
        _audit           = audit;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SiteResponseDto>>> Create([FromBody] CreateSiteDto dto)
    {
        try
        {
            var created = await _svc.CreateAsync(dto);

            _audit.Log(new AuditLogCreateDto
            {
                ActorUserId = GetUserId(),
                ActorName   = GetUserName(),
                ActorEmail  = GetUserEmail(),
                Action      = "SITE_CREATED",
                ServiceName = "ProtocolService",
                Description = $"Site '{created.Name}' ({created.Location}) registered",
                EntityId    = created.SiteId.ToString(),
                EntityName  = created.Name,
                IpAddress   = GetIp()
            });

            return StatusCode(201, ApiResponse<SiteResponseDto>.Ok(created, "Site registered successfully."));
        }
        catch (ArgumentException ex)       { return BadRequest(ApiResponse<SiteResponseDto>.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return Conflict(ApiResponse<SiteResponseDto>.Fail(ex.Message)); }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SiteResponseDto>>>> GetAll(
        [FromQuery] string? name,
        [FromQuery] string? location)
    {
        var list = await _svc.GetAllAsync(name, location);
        return Ok(ApiResponse<List<SiteResponseDto>>.Ok(list));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SiteResponseDto>>> GetById(Guid id)
    {
        var result = await _svc.GetByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<SiteResponseDto>.Fail("Site not found."));
        return Ok(ApiResponse<SiteResponseDto>.Ok(result));
    }

    [HttpGet("{siteId:guid}/protocols")]
    public async Task<ActionResult<ApiResponse<List<ProtocolSiteResponseDto>>>> GetProtocols(Guid siteId)
    {
        var list = await _protocolSiteSvc.GetBySiteAsync(siteId);
        return Ok(ApiResponse<List<ProtocolSiteResponseDto>>.Ok(list));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SiteResponseDto>>> Update(Guid id, [FromBody] UpdateSiteDto dto)
    {
        try
        {
            await _svc.UpdateAsync(id, dto);

            _audit.Log(new AuditLogCreateDto
            {
                ActorUserId = GetUserId(),
                ActorName   = GetUserName(),
                ActorEmail  = GetUserEmail(),
                Action      = "SITE_UPDATED",
                ServiceName = "ProtocolService",
                Description = $"Site {id} details updated",
                EntityId    = id.ToString(),
                IpAddress   = GetIp()
            });

            return Ok(ApiResponse<SiteResponseDto>.OkOnly("Site updated successfully."));
        }
        catch (ArgumentException ex)       { return BadRequest(ApiResponse<SiteResponseDto>.Fail(ex.Message)); }
        catch (KeyNotFoundException ex)    { return NotFound(ApiResponse<SiteResponseDto>.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return Conflict(ApiResponse<SiteResponseDto>.Fail(ex.Message)); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SiteResponseDto>>> Delete(Guid id)
    {
        try
        {
            await _svc.SoftDeleteAsync(id);

            _audit.Log(new AuditLogCreateDto
            {
                ActorUserId = GetUserId(),
                ActorName   = GetUserName(),
                ActorEmail  = GetUserEmail(),
                Action      = "SITE_DELETED",
                ServiceName = "ProtocolService",
                Description = $"Site {id} deactivated and all assignments closed",
                EntityId    = id.ToString(),
                IpAddress   = GetIp()
            });

            return Ok(ApiResponse<SiteResponseDto>.OkOnly("Site deleted successfully."));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<SiteResponseDto>.Fail(ex.Message)); }
    }

    // ── Helpers ──────────────────────────────────────────────
    private Guid?   GetUserId()    { var c = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub"); return c != null && Guid.TryParse(c.Value, out var g) ? g : null; }
    private string  GetUserName()  => User.FindFirst("name")?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
    private string? GetUserEmail() => User.FindFirst("email")?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value;
    private string? GetIp()        => HttpContext.Connection.RemoteIpAddress?.ToString();
}
