using Shared.CL.DTOs;

namespace Shared.CL.Services;

public interface IAuditClient
{
    void Log(AuditLogCreateDto dto);
}
