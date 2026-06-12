using Shared.CL.DTOs;

namespace AuditLogService.API.Repository;

public interface IAuditLogRepository
{
    Task<int>                  CreateLogAsync(AuditLogCreateDto dto);
    Task<IList<AuditLogListDto>> GetAllLogsAsync(int limit = 500);
    Task<IList<AuditLogListDto>> GetLogsByServiceAsync(string serviceName);
    Task<IList<AuditLogListDto>> GetLogsByUserAsync(Guid userId);
    Task<IList<AuditLogListDto>> GetErrorLogsAsync();
}
