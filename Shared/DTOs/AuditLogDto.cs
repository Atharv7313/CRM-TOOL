namespace Shared.DTOs;

public class AuditLogDto
{
    public int LogId { get; set; }
    public string ActorName { get; set; } = null!;
    public string ActorEmail { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? TargetUserName { get; set; }
    public string? IpAddress { get; set; }
    public bool IsSuccess { get; set; }
    public DateTime CreatedAt { get; set; }
}
