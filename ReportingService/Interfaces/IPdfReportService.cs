using Shared.DTOs;

namespace ReportingService.Interfaces;

public interface IPdfReportService
{
    byte[] GenerateDashboardPdf(DashboardSummaryDto dashboard, IEnumerable<KpiReportDto> reports);
    byte[] GenerateSingleReportPdf(KpiReportDto report);
}