using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ReportingService.Hubs;
using ReportingService.Interfaces;
using ReportingService.Services;
using Shared.DTOs;

namespace ReportingService.Controllers;

[ApiController]
[Route("api/reporting")]
[Authorize]
public class KpiReportController(
    IKpiReportRepository repo,
    IPdfReportService pdfService,
    IHubContext<ReportingHub, IReportingClient> hub) : ControllerBase
{
    // GET api/reporting/dashboard
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard() =>
        Ok(await repo.GetDashboardSummaryAsync());

    // GET api/reporting/reports
    [HttpGet("reports")]
    public async Task<IActionResult> GetAll() =>
        Ok(await repo.GetAllAsync());

    // GET api/reporting/reports/{id}
    [HttpGet("reports/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var report = await repo.GetByIdAsync(id);
        return report is null ? NotFound() : Ok(report);
    }

    // GET api/reporting/reports/scope/{scope}
    [HttpGet("reports/scope/{scope}")]
    public async Task<IActionResult> GetByScope(string scope)
    {
        if (!Enum.TryParse<KpiScope>(scope, true, out var kpiScope))
            return BadRequest($"Invalid scope. Valid values: {string.Join(", ", Enum.GetNames<KpiScope>())}");
        return Ok(await repo.GetByScopeAsync(kpiScope));
    }

    // GET api/reporting/reports/protocol/{protocolId}
    [HttpGet("reports/protocol/{protocolId:guid}")]
    public async Task<IActionResult> GetByProtocol(Guid protocolId) =>
        Ok(await repo.GetByProtocolAsync(protocolId));

    // GET api/reporting/reports/user/{userId}
    [HttpGet("reports/user/{userId:guid}")]
    public async Task<IActionResult> GetByUser(Guid userId) =>
        Ok(await repo.GetByUserAsync(userId));

    // POST api/reporting/reports
    [HttpPost("reports")]
    public async Task<IActionResult> Create([FromBody] CreateKpiReportDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var report = await repo.CreateAsync(dto);

            // Real-time push
            await hub.Clients.All.ReceiveNewReport(report);
            var dashboard = await repo.GetDashboardSummaryAsync();
            await hub.Clients.All.ReceiveDashboardUpdate(dashboard);

            // Push any new critical alerts
            foreach (var alert in dashboard.Alerts.Where(a =>
                a.Level == KpiAlertLevel.Critical))
                await hub.Clients.All.ReceiveAlert(alert);

            return CreatedAtAction(nameof(GetById), new { id = report.ReportId }, report);
        }
        catch (ValidationException ex)
        {
            return UnprocessableEntity(new { error = ex.Message });
        }
    }

    // PUT api/reporting/reports/{id}
    [HttpPut("reports/{id:guid}")]
    [Authorize(Roles = "DATA_MANAGER,ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateKpiReportDto dto)
    {
        var report = await repo.UpdateAsync(id, dto);
        if (report is null) return NotFound();

        var dashboard = await repo.GetDashboardSummaryAsync();
        await hub.Clients.All.ReceiveDashboardUpdate(dashboard);

        return Ok(report);
    }

    // DELETE api/reporting/reports/{id}
    [HttpDelete("reports/{id:guid}")]
    [Authorize(Roles = "ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await repo.DeleteAsync(id);
        if (!deleted) return NotFound();

        var dashboard = await repo.GetDashboardSummaryAsync();
        await hub.Clients.All.ReceiveDashboardUpdate(dashboard);

        return NoContent();
    }

    // ── PDF Download endpoints ────────────────────────────────────────────────

    // GET api/reporting/pdf/dashboard
    [HttpGet("pdf/dashboard")]
    public async Task<IActionResult> DownloadDashboardPdf()
    {
        var dashboard = await repo.GetDashboardSummaryAsync();
        var reports = await repo.GetAllAsync();
        var bytes = pdfService.GenerateDashboardPdf(dashboard, reports);

        return File(bytes, "application/pdf",
            $"LifeSci360_Dashboard_{DateTime.UtcNow:yyyyMMdd_HHmm}.pdf");
    }

    // GET api/reporting/pdf/report/{id}
    [HttpGet("pdf/report/{id:guid}")]
    public async Task<IActionResult> DownloadReportPdf(Guid id)
    {
        var report = await repo.GetByIdAsync(id);
        if (report is null) return NotFound();

        var bytes = pdfService.GenerateSingleReportPdf(report);
        return File(bytes, "application/pdf",
            $"KPI_Report_{report.Scope}_{report.GeneratedAt:yyyyMMdd}.pdf");
    }
}