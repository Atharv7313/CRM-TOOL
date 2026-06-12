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
public class LabResultsController : ControllerBase
{
    private readonly ILabResultService _service;
    private readonly IAuditClient      _audit;

    public LabResultsController(ILabResultService service, IAuditClient audit)
    {
        _service = service;
        _audit   = audit;
    }

    [HttpGet("sample/{sampleId:guid}")]
    public async Task<ActionResult<ApiResponse<IList<LabResultListDto>>>> GetBySample(Guid sampleId)
    {
        IList<LabResultListDto> results = await _service.GetBySampleAsync(sampleId);
        return Ok(ApiResponse<IList<LabResultListDto>>.Success(results, $"{results.Count} result(s) found."));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<LabResultListDto>>> GetById(Guid id)
    {
        LabResultListDto? result = await _service.GetByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<LabResultListDto>.Fail("Lab result not found."));
        return Ok(ApiResponse<LabResultListDto>.Success(result));
    }

    [HttpPost]
    [Authorize(Roles = "LAB_TECHNICIAN,ADMIN,SYSTEM_ADMIN")]
    public async Task<ActionResult<ApiResponse<LabResultListDto>>> Create([FromBody] LabResultCreateDto dto)
    {
        LabResultListDto created = await _service.CreateAsync(dto);

        _audit.Log(new AuditLogCreateDto
        {
            ActorUserId = GetUserId(),
            ActorName   = GetUserName(),
            ActorEmail  = GetUserEmail(),
            Action      = "LAB_RESULT_CREATED",
            ServiceName = "SampleService",
            Description = $"Lab result recorded for sample {dto.SampleId} — test: {dto.TestType}",
            EntityId    = created.ResultId.ToString(),
            EntityName  = dto.TestType,
            IpAddress   = GetIp()
        });

        return CreatedAtAction(nameof(GetById), new { id = created.ResultId },
            ApiResponse<LabResultListDto>.Success(created, "Lab result recorded successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "LAB_TECHNICIAN,ADMIN,SYSTEM_ADMIN")]
    public async Task<ActionResult<ApiResponse<LabResultListDto>>> Update(Guid id, [FromBody] LabResultUpdateDto dto)
    {
        LabResultListDto? updated = await _service.UpdateAsync(id, dto);
        if (updated == null)
            return NotFound(ApiResponse<LabResultListDto>.Fail("Lab result not found."));

        _audit.Log(new AuditLogCreateDto
        {
            ActorUserId = GetUserId(),
            ActorName   = GetUserName(),
            ActorEmail  = GetUserEmail(),
            Action      = "LAB_RESULT_UPDATED",
            ServiceName = "SampleService",
            Description = $"Lab result {id} updated",
            EntityId    = id.ToString(),
            IpAddress   = GetIp()
        });

        return Ok(ApiResponse<LabResultListDto>.Success(updated, "Lab result updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "ADMIN,SYSTEM_ADMIN")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(Guid id)
    {
        bool ok = await _service.DeleteAsync(id);
        if (!ok) return NotFound(ApiResponse<string>.Fail("Lab result not found."));

        _audit.Log(new AuditLogCreateDto
        {
            ActorUserId = GetUserId(),
            ActorName   = GetUserName(),
            ActorEmail  = GetUserEmail(),
            Action      = "LAB_RESULT_DELETED",
            ServiceName = "SampleService",
            Description = $"Lab result {id} deleted",
            EntityId    = id.ToString(),
            IpAddress   = GetIp()
        });

        return Ok(ApiResponse<string>.Success("deleted", "Lab result deleted successfully."));
    }

    // ── Helpers ──────────────────────────────────────────────
    private Guid?   GetUserId()    { var c = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub"); return c != null && Guid.TryParse(c.Value, out var g) ? g : null; }
    private string  GetUserName()  => User.FindFirst("name")?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
    private string? GetUserEmail() => User.FindFirst("email")?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value;
    private string? GetIp()        => HttpContext.Connection.RemoteIpAddress?.ToString();
}
