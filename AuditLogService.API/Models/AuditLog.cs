namespace AuditLogService.API.Models;

public class AuditLog
{
    public int      Id           { get; set; }
    public Guid?    ActorUserId  { get; set; }
    public string   ActorName    { get; set; } = string.Empty;
    public string?  ActorEmail   { get; set; }
    public string   Action       { get; set; } = string.Empty;
    public string   ServiceName  { get; set; } = string.Empty;
    public string?  Description  { get; set; }
    public string?  EntityId     { get; set; }
    public string?  EntityName   { get; set; }
    public string?  IpAddress    { get; set; }
    public bool     IsSuccess    { get; set; } = true;
    public string?  ErrorMessage { get; set; }
    public DateTime CreatedAt    { get; set; } = DateTime.UtcNow;
}
