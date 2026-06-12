using Shared.DTOs;

namespace NotificationService.Interfaces;

/// <summary>
/// Business logic for notifications: fan-out across channels, real-time push,
/// broadcast to roles, history, and escalation.
/// </summary>
public interface INotificationService
{
    Task<IEnumerable<NotificationDto>> GetForUserAsync(Guid userId, string? status, string? category);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task<NotificationHistoryDto> GetHistoryAsync(Guid userId, DateTime? from, DateTime? to);

    /// <summary>Creates one row per requested channel for a single user, pushes the in-app one live.</summary>
    Task<List<NotificationDto>> CreateAsync(CreateNotificationDto dto, Guid? sentByUserId = null);

    /// <summary>Creates notifications for every recipient (by role, or all active users).</summary>
    Task<List<NotificationDto>> BroadcastAsync(BroadcastNotificationDto dto, Guid? sentByUserId = null);

    /// <summary>All notifications sent by a specific user.</summary>
    Task<List<NotificationDto>> GetSentAsync(Guid sentByUserId);

    Task<bool> MarkReadAsync(Guid id, Guid userId);
    Task<int> MarkAllReadAsync(Guid userId);
    Task<bool> DeleteAsync(Guid id, Guid userId);

    /// <summary>Finds stale UNREAD escalatable notifications, marks them ESCALATED and re-pushes. Returns count escalated.</summary>
    Task<int> RunEscalationAsync();
}
