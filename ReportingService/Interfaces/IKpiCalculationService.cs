using Shared.DTOs;

namespace ReportingService.Interfaces;

public interface IKpiCalculationService
{
    Task<KpiMetrics> CalculateCurrentAsync(Guid? protocolId);
    Task<KpiMetrics> CalculatePreviousPeriodAsync(Guid? protocolId);
}