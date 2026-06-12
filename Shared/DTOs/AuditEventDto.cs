namespace Shared.DTOs;

public class AuditEventDto
{
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
}