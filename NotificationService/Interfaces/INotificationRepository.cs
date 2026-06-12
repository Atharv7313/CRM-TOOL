using NotificationService.Data.Entities;
using Shared.DTOs;

namespace NotificationService.Interfaces;

/// <summary>Data-access contract for notifications. Implementations talk to the DB only.</summary>
public interface INotificationRepository
{
    Task<IEnumerable<NotificationDto>> GetForUserAsync(Guid userId, string? status, string? category);
    Task<NotificationDto?> GetByIdAsync(Guid id);
    Task<int> GetUnreadCountAsync(Guid userId);

    /// <summary>Persists the given notification rows and returns them as DTOs.</summary>
    Task<List<NotificationDto>> AddRangeAsync(IEnumerable<Notification> notifications);

    /// <summary>Marks a single notification READ (only if it belongs to the user). Returns false if not found.</summary>
    Task<bool> MarkReadAsync(Guid id, Guid userId);

    /// <summary>Marks every UNREAD notification for the user as READ. Returns how many were updated.</summary>
    Task<int> MarkAllReadAsync(Guid userId);

    Task<bool> DeleteAsync(Guid id, Guid userId);

    Task<NotificationHistoryDto> GetHistoryAsync(Guid userId, DateTime from, DateTime to);

    /// <summary>UNREAD in-app notifications in the given categories created before olderThanUtc.</summary>
    Task<List<Notification>> GetEscalatableAsync(IReadOnlyCollection<string> categories, DateTime olderThanUtc);

    Task MarkEscalatedAsync(IReadOnlyCollection<Guid> ids);

    /// <summary>User ids for the given role; when role is null/empty returns all active users.</summary>
    Task<List<Guid>> ResolveRecipientsAsync(string? role);

    /// <summary>All notifications sent by a specific user (most recent first).</summary>
    Task<List<NotificationDto>> GetSentByUserAsync(Guid sentByUserId);
}
