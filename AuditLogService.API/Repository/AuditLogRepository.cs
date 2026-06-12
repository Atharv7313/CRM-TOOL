using AuditLogService.API.Data;
using AuditLogService.API.Models;
using Microsoft.EntityFrameworkCore;
using Shared.CL.DTOs;

namespace AuditLogService.API.Repository;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AuditLogDbContext _db;

    public AuditLogRepository(AuditLogDbContext db) => _db = db;

    public async Task<int> CreateLogAsync(AuditLogCreateDto dto)
    {
        try
        {
            var log = new AuditLog
            {
                ActorUserId  = dto.ActorUserId,
                ActorName    = dto.ActorName,
                ActorEmail   = dto.ActorEmail,
                Action       = dto.Action,
                ServiceName  = dto.ServiceName,
                Description  = dto.Description,
                EntityId     = dto.EntityId,
                EntityName   = dto.EntityName,
                IpAddress    = dto.IpAddress,
                IsSuccess    = dto.IsSuccess,
                ErrorMessage = dto.ErrorMessage,
                CreatedAt    = DateTime.UtcNow
            };
            await _db.AuditLogs.AddAsync(log);
            await _db.SaveChangesAsync();
            return log.Id;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuditLogRepository] Save failed: {ex.Message}");
            return -1;
        }
    }

    public async Task<IList<AuditLogListDto>> GetAllLogsAsync(int limit = 500)
        => await _db.AuditLogs
            .AsNoTracking()
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .Select(l => Map(l))
            .ToListAsync();

    public async Task<IList<AuditLogListDto>> GetLogsByServiceAsync(string serviceName)
        => await _db.AuditLogs
            .AsNoTracking()
            .Where(l => l.ServiceName == serviceName)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => Map(l))
            .ToListAsync();

    public async Task<IList<AuditLogListDto>> GetLogsByUserAsync(Guid userId)
        => await _db.AuditLogs
            .AsNoTracking()
            .Where(l => l.ActorUserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => Map(l))
            .ToListAsync();

    public async Task<IList<AuditLogListDto>> GetErrorLogsAsync()
        => await _db.AuditLogs
            .AsNoTracking()
            .Where(l => !l.IsSuccess)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => Map(l))
            .ToListAsync();

    private static AuditLogListDto Map(AuditLog l) => new()
    {
        Id           = l.Id,
        ActorUserId  = l.ActorUserId,
        ActorName    = l.ActorName,
        ActorEmail   = l.ActorEmail,
        Action       = l.Action,
        ServiceName  = l.ServiceName,
        Description  = l.Description,
        EntityId     = l.EntityId,
        EntityName   = l.EntityName,
        IpAddress    = l.IpAddress,
        IsSuccess    = l.IsSuccess,
        ErrorMessage = l.ErrorMessage,
        CreatedAt    = l.CreatedAt
    };
}
