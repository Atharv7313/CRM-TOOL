using Microsoft.EntityFrameworkCore;
using ReportingService.Data;
using ReportingService.Data.Entities;
using ReportingService.Interfaces;
using ReportingService.Services;
using Shared.DTOs;
using System.Text.Json;

namespace ReportingService.Repositories;

public class KpiReportRepository(
    ServicesDbContext db,
    IKpiCalculationService calculator,
    IReportValidationService validator) : IKpiReportRepository
{
    private static readonly JsonSerializerOptions _json = new()
    { PropertyNameCaseInsensitive = true };

    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<IEnumerable<KpiReportDto>> GetAllAsync() =>
        await db.KpiReports
            .AsNoTracking()
            .Include(r => r.Protocol)
            .Include(r => r.GeneratedByUser)
            .OrderByDescending(r => r.GeneratedAt)
            .Select(r => ToDto(r))
            .ToListAsync();

    public async Task<KpiReportDto?> GetByIdAsync(Guid reportId)
    {
        var r = await db.KpiReports
            .AsNoTracking()
            .Include(r => r.Protocol)
            .Include(r => r.GeneratedByUser)
            .FirstOrDefaultAsync(r => r.ReportId == reportId);
        return r is null ? null : ToDto(r);
    }

    public async Task<IEnumerable<KpiReportDto>> GetByScopeAsync(KpiScope scope) =>
        await db.KpiReports
            .AsNoTracking()
            .Include(r => r.Protocol)
            .Include(r => r.GeneratedByUser)
            .Where(r => r.Scope == scope.ToString())
            .OrderByDescending(r => r.GeneratedAt)
            .Select(r => ToDto(r))
            .ToListAsync();

    public async Task<IEnumerable<KpiReportDto>> GetByProtocolAsync(Guid protocolId) =>
        await db.KpiReports
            .AsNoTracking()
            .Include(r => r.Protocol)
            .Include(r => r.GeneratedByUser)
            .Where(r => r.ProtocolId == protocolId)
            .OrderByDescending(r => r.GeneratedAt)
            .Select(r => ToDto(r))
            .ToListAsync();

    public async Task<IEnumerable<KpiReportDto>> GetByUserAsync(Guid userId) =>
        await db.KpiReports
            .AsNoTracking()
            .Include(r => r.Protocol)
            .Include(r => r.GeneratedByUser)
            .Where(r => r.GeneratedByUserId == userId)
            .OrderByDescending(r => r.GeneratedAt)
            .Select(r => ToDto(r))
            .ToListAsync();

    // ── Commands ──────────────────────────────────────────────────────────────

    public async Task<KpiReportDto> CreateAsync(CreateKpiReportDto dto)
    {
        // Business rules first
        await validator.ValidateCreateAsync(dto);

        // Calculate KPIs from real DB data — caller cannot inject numbers
        var metrics = await calculator.CalculateCurrentAsync(dto.ProtocolId);

        var report = new KpiReport
        {
            ReportId = Guid.NewGuid(),
            ProtocolId = dto.ProtocolId,
            GeneratedByUserId = dto.GeneratedByUserId,
            Scope = dto.Scope.ToString(),
            Metrics = JsonSerializer.Serialize(metrics),
            GeneratedAt = DateTime.UtcNow
        };

        db.KpiReports.Add(report);
        await db.SaveChangesAsync();

        // Reload with navigation properties for the response
        return (await GetByIdAsync(report.ReportId))!;
    }

    public async Task<KpiReportDto?> UpdateAsync(Guid reportId, UpdateKpiReportDto dto)
    {
        var report = await db.KpiReports.FindAsync(reportId);
        if (report is null) return null;

        // Recalculate metrics fresh on every update
        var metrics = await calculator.CalculateCurrentAsync(dto.ProtocolId);

        report.ProtocolId = dto.ProtocolId;
        report.Scope = dto.Scope.ToString();
        report.Metrics = JsonSerializer.Serialize(metrics);
        report.GeneratedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return (await GetByIdAsync(reportId))!;
    }

    public async Task<bool> DeleteAsync(Guid reportId)
    {
        var report = await db.KpiReports.FindAsync(reportId);
        if (report is null) return false;
        db.KpiReports.Remove(report);
        await db.SaveChangesAsync();
        return true;
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
    {
        // Calculate current and previous period KPIs in parallel
        var current = await calculator.CalculateCurrentAsync(null);
        var previous = await calculator.CalculatePreviousPeriodAsync(null);

        // Trend = current minus previous
        var trends = new KpiTrendDto(
            EnrollmentRateChange: current.EnrollmentRate - previous.EnrollmentRate,
            SampleProcessingRateChange: current.SampleProcessingRate - previous.SampleProcessingRate,
            ComplianceScoreChange: current.ComplianceScore - previous.ComplianceScore,
            SitePerformanceScoreChange: current.SitePerformanceScore - previous.SitePerformanceScore
        );

        // Aggregate counts from real tables
        var totalReports = await db.KpiReports.CountAsync();
        var activeProtocols = await db.Protocols.CountAsync(p => p.Status == "UPCOMING" || p.Status == "ONGOING");
        var totalEnrolledPatients = await db.PatientEnrollments.CountAsync(e => e.EnrollmentStatus == "ACTIVE");
        var totalSamplesProcessed = await db.Samples.CountAsync(s => s.Status == "TESTED" || s.Status == "ANALYZED");

        // Reports grouped by scope
        var byScope = await db.KpiReports
            .GroupBy(r => r.Scope)
            .Select(g => new ScopeCount(g.Key, g.Count()))
            .ToListAsync();

        // Generate alerts from current KPI values
        var alerts = AlertService.Evaluate(current);

        return new DashboardSummaryDto(
            CurrentKpis: current,
            PreviousPeriodKpis: previous,
            Trends: trends,
            ReportsByScope: byScope,
            TotalReports: totalReports,
            ActiveProtocols: activeProtocols,
            TotalEnrolledPatients: totalEnrolledPatients,
            TotalSamplesProcessed: totalSamplesProcessed,
            Alerts: alerts,
            LastRefreshed: DateTime.UtcNow
        );
    }

    // ── Mapper ────────────────────────────────────────────────────────────────

    private static KpiReportDto ToDto(KpiReport r)
    {
        KpiMetrics? parsed = null;
        if (!string.IsNullOrWhiteSpace(r.Metrics))
        {
            try { parsed = JsonSerializer.Deserialize<KpiMetrics>(r.Metrics, _json); }
            catch { /* leave null */ }
        }

        return new KpiReportDto(
            ReportId: r.ReportId,
            ProtocolId: r.ProtocolId,
            ProtocolTitle: r.Protocol?.Title,
            GeneratedByUserId: r.GeneratedByUserId,
            GeneratedByUserName: r.GeneratedByUser?.Name ?? "Unknown",
            Scope: r.Scope,
            ParsedMetrics: parsed,
            RawMetrics: r.Metrics,
            GeneratedAt: r.GeneratedAt
        );
    }
}