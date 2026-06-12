using ReportingService.Interfaces;
using Shared.DTOs;

namespace ReportingService.Services;

public static class AlertService
{
    private const double WarningThreshold = 70.0;
    private const double CriticalThreshold = 50.0;

    public static List<AlertDto> Evaluate(KpiMetrics metrics)
    {
        var alerts = new List<AlertDto>();

        Check(alerts, "Enrollment Rate", metrics.EnrollmentRate);
        Check(alerts, "Sample Processing Rate", metrics.SampleProcessingRate);
        Check(alerts, "Compliance Score", metrics.ComplianceScore);
        Check(alerts, "Site Performance", metrics.SitePerformanceScore);

        return alerts;
    }

    private static void Check(List<AlertDto> alerts, string name, double value)
    {
        if (value < CriticalThreshold)
            alerts.Add(new AlertDto(name, value, KpiAlertLevel.Critical,
                $"{name} is critically low at {value:F1}%. Immediate action required."));
        else if (value < WarningThreshold)
            alerts.Add(new AlertDto(name, value, KpiAlertLevel.Warning,
                $"{name} is below target at {value:F1}%. Review recommended."));
    }
}