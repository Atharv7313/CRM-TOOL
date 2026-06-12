namespace Shared.Models;

public class Notification
{
    public int NotificationID { get; set; }
    public int UserID { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Milestone | LabResult | Compliance
    public string Status { get; set; } = "Unread";       // Unread | Read | Dismissed
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}