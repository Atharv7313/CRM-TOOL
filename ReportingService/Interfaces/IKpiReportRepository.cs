using Shared.DTOs;

namespace ReportingService.Interfaces;

public interface IKpiReportRepository
{
    Task<IEnumerable<KpiReportDto>> GetAllAsync();
    Task<KpiReportDto?> GetByIdAsync(Guid reportId);
    Task<IEnumerable<KpiReportDto>> GetByScopeAsync(KpiScope scope);
    Task<IEnumerable<KpiReportDto>> GetByProtocolAsync(Guid protocolId);
    Task<IEnumerable<KpiReportDto>> GetByUserAsync(Guid userId);
    Task<KpiReportDto> CreateAsync(CreateKpiReportDto dto);
    Task<KpiReportDto?> UpdateAsync(Guid reportId, UpdateKpiReportDto dto);
    Task<bool> DeleteAsync(Guid reportId);
    Task<DashboardSummaryDto> GetDashboardSummaryAsync();
}