namespace Shared.DTOs;

// ── Enums ─────────────────────────────────────────────────────────────────────

public enum KpiScope
{
    Enrollment,
    SampleProcessing,
    Compliance,
    SitePerformance
}

public enum KpiAlertLevel
{
    Normal,
    Warning,   // < 70%
    Critical   // < 50%
}

// ── Core KPI value object ─────────────────────────────────────────────────────

public record KpiMetrics(
    double EnrollmentRate,
    double SampleProcessingRate,
    double ComplianceScore,
    double SitePerformanceScore
);

// ── Response DTOs ─────────────────────────────────────────────────────────────

public record KpiReportDto(
    Guid ReportId,
    Guid? ProtocolId,
    string? ProtocolTitle,
    Guid GeneratedByUserId,
    string GeneratedByUserName,
    string Scope,
    KpiMetrics? ParsedMetrics,
    string? RawMetrics,
    DateTime GeneratedAt
);

public record DashboardSummaryDto(
    KpiMetrics CurrentKpis,
    KpiMetrics PreviousPeriodKpis,       // for trend comparison
    KpiTrendDto Trends,
    List<ScopeCount> ReportsByScope,
    int TotalReports,
    int ActiveProtocols,
    int TotalEnrolledPatients,
    int TotalSamplesProcessed,
    List<AlertDto> Alerts,
    DateTime LastRefreshed
);

public record KpiTrendDto(
    double EnrollmentRateChange,       // % change vs previous period
    double SampleProcessingRateChange,
    double ComplianceScoreChange,
    double SitePerformanceScoreChange
);

public record ScopeCount(string Scope, int Count);

public record AlertDto(
    string KpiName,
    double CurrentValue,
    KpiAlertLevel Level,
    string Message
);

// ── Request DTOs ──────────────────────────────────────────────────────────────

public record CreateKpiReportDto(
    Guid? ProtocolId,
    Guid GeneratedByUserId,
    KpiScope Scope
// Metrics are CALCULATED — not passed in by caller
);

public record UpdateKpiReportDto(
    Guid? ProtocolId,
    KpiScope Scope
);