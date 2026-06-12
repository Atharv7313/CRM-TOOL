namespace Shared.DTOs;

public class ActiveSessionDto
{
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string? IpAddress { get; set; }
    public DateTime LoginTime { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Status { get; set; } = "Active";
}
