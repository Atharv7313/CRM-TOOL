using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SampleService.Services;
using Shared.CL;
using Shared.CL.DTOs;
using Shared.CL.Services;
using System.Security.Claims;

namespace SampleService.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class SamplesController : ControllerBase
{
    private readonly ISampleService _service;
    private readonly IAuditClient   _audit;

    public SamplesController(ISampleService service, IAuditClient audit)
    {
        _service = service;
        _audit   = audit;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IList<SampleListDto>>>> GetAll()
    {
        IList<SampleListDto> samples = await _service.GetAllAsync();
        return Ok(ApiResponse<IList<SampleListDto>>.Success(samples, $"{samples.Count} sample(s) found."));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SampleListDto>>> GetById(Guid id)
    {
        SampleListDto? sample = await _service.GetByIdAsync(id);
        if (sample == null)
            return NotFound(ApiResponse<SampleListDto>.Fail("Sample not found."));
        return Ok(ApiResponse<SampleListDto>.Success(sample));
    }

    [HttpGet("enrollment/{enrollmentId:guid}")]
    public async Task<ActionResult<ApiResponse<IList<SampleListDto>>>> GetByEnrollment(Guid enrollmentId)
    {
        IList<SampleListDto> samples = await _service.GetByEnrollmentAsync(enrollmentId);
        return Ok(ApiResponse<IList<SampleListDto>>.Success(samples, $"{samples.Count} sample(s) found."));
    }

    [HttpPost]
    [Authorize(Roles = "LAB_TECHNICIAN,ADMIN,SYSTEM_ADMIN")]
    public async Task<ActionResult<ApiResponse<SampleListDto>>> Create([FromBody] SampleCreateDto dto)
    {
        SampleListDto created = await _service.CreateAsync(dto);

        _audit.Log(new AuditLogCreateDto
        {
            ActorUserId = GetUserId(),
            ActorName   = GetUserName(),
            ActorEmail  = GetUserEmail(),
            Action      = "SAMPLE_CREATED",
            ServiceName = "SampleService",
            Description = $"Sample '{created.SampleType}' collected (enrollment {dto.EnrollmentId})",
            EntityId    = created.SampleId.ToString(),
            EntityName  = created.SampleType,
            IpAddress   = GetIp()
        });

        return CreatedAtAction(nameof(GetById), new { id = created.SampleId },
            ApiResponse<SampleListDto>.Success(created, "Sample created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "LAB_TECHNICIAN,ADMIN,SYSTEM_ADMIN,RESEARCH_SCIENTIST")]
    public async Task<ActionResult<ApiResponse<SampleListDto>>> Update(Guid id, [FromBody] SampleUpdateDto dto)
    {
        SampleListDto? updated = await _service.UpdateAsync(id, dto);
        if (updated == null)
            return NotFound(ApiResponse<SampleListDto>.Fail("Sample not found."));

        _audit.Log(new AuditLogCreateDto
        {
            ActorUserId = GetUserId(),
            ActorName   = GetUserName(),
            ActorEmail  = GetUserEmail(),
            Action      = "SAMPLE_UPDATED",
            ServiceName = "SampleService",
            Description = $"Sample {id} updated",
            EntityId    = id.ToString(),
            IpAddress   = GetIp()
        });

        return Ok(ApiResponse<SampleListDto>.Success(updated, "Sample updated successfully."));
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "LAB_TECHNICIAN,RESEARCH_SCIENTIST,ADMIN,SYSTEM_ADMIN")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateStatus(Guid id, [FromBody] string status)
    {
        try
        {
            bool ok = await _service.UpdateStatusAsync(id, status);
            if (!ok) return NotFound(ApiResponse<string>.Fail("Sample not found."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<string>.Fail(ex.Message));
        }

        _audit.Log(new AuditLogCreateDto
        {
            ActorUserId = GetUserId(),
            ActorName   = GetUserName(),
            ActorEmail  = GetUserEmail(),
            Action      = "SAMPLE_STATUS_UPDATED",
            ServiceName = "SampleService",
            Description = $"Sample {id} status → '{status}'",
            EntityId    = id.ToString(),
            IpAddress   = GetIp()
        });

        return Ok(ApiResponse<string>.Success(status, "Sample status updated."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "ADMIN,SYSTEM_ADMIN")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(Guid id)
    {
        bool ok = await _service.DeleteAsync(id);
        if (!ok) return NotFound(ApiResponse<string>.Fail("Sample not found."));

        _audit.Log(new AuditLogCreateDto
        {
            ActorUserId = GetUserId(),
            ActorName   = GetUserName(),
            ActorEmail  = GetUserEmail(),
            Action      = "SAMPLE_DELETED",
            ServiceName = "SampleService",
            Description = $"Sample {id} permanently deleted",
            EntityId    = id.ToString(),
            IpAddress   = GetIp()
        });

        return Ok(ApiResponse<string>.Success("deleted", "Sample deleted successfully."));
    }

    // ── Helpers ──────────────────────────────────────────────
    private Guid?   GetUserId()    { var c = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub"); return c != null && Guid.TryParse(c.Value, out var g) ? g : null; }
    private string  GetUserName()  => User.FindFirst("name")?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
    private string? GetUserEmail() => User.FindFirst("email")?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value;
    private string? GetIp()        => HttpContext.Connection.RemoteIpAddress?.ToString();
}
