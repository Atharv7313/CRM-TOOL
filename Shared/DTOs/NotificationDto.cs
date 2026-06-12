namespace Shared.DTOs;

// ── Notification value object (matches the Notifications table) ────────────────
public record NotificationDto(
    Guid NotificationId,
    Guid UserId,
    string Message,
    string Category,
    string Channel,        // IN_APP | EMAIL | SMS
    string Status,         // UNREAD | READ | ESCALATED | ARCHIVED
    DateTime CreatedAt,
    DateTime? ReadAt,
    Guid? SentByUserId = null,
    string? RecipientName = null
);

// ── Requests ──────────────────────────────────────────────────────────────────

// Create a notification for ONE user, fanned out across one or more channels.
public record CreateNotificationDto(
    Guid UserId,
    string Message,
    string Category,
    List<string> Channels   // IN_APP | EMAIL | SMS  (one row is created per channel)
);

// Send the same notification to MANY users — everyone with the given role,
// or every active user when Role is null/empty.
public record BroadcastNotificationDto(
    string Message,
    string Category,
    List<string> Channels,
    string? Role
);

public record UpdateNotificationStatusDto(string Status);

// ── History / summary ───────────────────────────────────────────────────────
public record NotificationHistoryDto(
    Guid UserId,
    DateTime From,
    DateTime To,
    int TotalCount,
    int ReadCount,
    int UnreadCount,
    int EscalatedCount,
    List<CategorySummary> ByCategory
);

public record CategorySummary(string Category, int Count);

public record UnreadCountDto(int Count);
