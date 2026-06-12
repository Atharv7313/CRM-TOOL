using Shared.DTOs;

namespace ReportingService.Interfaces;

public interface IReportValidationService
{
    Task ValidateCreateAsync(CreateKpiReportDto dto);
}