using Microsoft.AspNetCore.SignalR;
using NotificationService.Data.Entities;
using NotificationService.Hubs;
using NotificationService.Interfaces;
using Shared.DTOs;

namespace NotificationService.Services;

/// <summary>
/// Business logic for notifications: fan-out across channels, real-time push,
/// broadcast to roles, history, and escalation.
/// Registered in DI as NotificationService.Services.NotificationService.
/// </summary>
public class NotificationService(
    INotificationRepository repo,
    IHubContext<NotificationHub, INotificationClient> hub,
    IConfiguration config,
    ILogger<NotificationService> logger) : INotificationService
{
    public Task<IEnumerable<NotificationDto>> GetForUserAsync(Guid userId, string? status, string? category)
        => repo.GetForUserAsync(userId, status, category);

    public Task<int> GetUnreadCountAsync(Guid userId)
        => repo.GetUnreadCountAsync(userId);

    public Task<NotificationHistoryDto> GetHistoryAsync(Guid userId, DateTime? from, DateTime? to)
    {
        var toUtc = to ?? DateTime.UtcNow;
        var fromUtc = from ?? toUtc.AddDays(-30);
        return repo.GetHistoryAsync(userId, fromUtc, toUtc);
    }

    public async Task<List<NotificationDto>> CreateAsync(CreateNotificationDto dto, Guid? sentByUserId = null)
        => await CreateForUserAsync(dto.UserId, dto.Message, dto.Category, dto.Channels, sentByUserId);

    public async Task<List<NotificationDto>> BroadcastAsync(BroadcastNotificationDto dto, Guid? sentByUserId = null)
    {
        var recipients = await repo.ResolveRecipientsAsync(dto.Role);
        logger.LogInformation(
            "Broadcasting notification (category {Category}) to {Count} recipient(s) for role {Role}",
            dto.Category, recipients.Count, dto.Role ?? "<all active>");

        var created = new List<NotificationDto>();
        foreach (var userId in recipients)
        {
            var rows = await CreateForUserAsync(userId, dto.Message, dto.Category, dto.Channels, sentByUserId);
            created.AddRange(rows);
        }
        return created;
    }

    public Task<List<NotificationDto>> GetSentAsync(Guid sentByUserId)
        => repo.GetSentByUserAsync(sentByUserId);

    public async Task<bool> MarkReadAsync(Guid id, Guid userId)
    {
        var ok = await repo.MarkReadAsync(id, userId);
        if (ok)
            await PushUnreadCountAsync(userId);
        return ok;
    }

    public async Task<int> MarkAllReadAsync(Guid userId)
    {
        var count = await repo.MarkAllReadAsync(userId);
        await PushUnreadCountAsync(userId);
        return count;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId)
    {
        var ok = await repo.DeleteAsync(id, userId);
        if (ok)
            await PushUnreadCountAsync(userId);
        return ok;
    }

    public async Task<int> RunEscalationAsync()
    {
        var minutes = config.GetValue<int?>("Notifications:EscalateAfterMinutes") ?? 60;
        var categories = config.GetSection("Notifications:EscalateCategories").Get<string[]>()
            ?? new[] { "COMPLIANCE_DEADLINE", "LAB_RESULT" };

        var cutoff = DateTime.UtcNow.AddMinutes(-minutes);
        var stale = await repo.GetEscalatableAsync(categories, cutoff);
        if (stale.Count == 0)
            return 0;

        await repo.MarkEscalatedAsync(stale.Select(n => n.NotificationId).ToList());

        foreach (var n in stale)
        {
            var dto = new NotificationDto(
                n.NotificationId,
                n.UserId,
                n.Message,
                n.Category,
                n.Channel,
                "ESCALATED",
                n.CreatedAt,
                n.ReadAt);
            await hub.Clients.Group(n.UserId.ToString()).ReceiveNotification(dto);
        }

        logger.LogWarning("Escalated {Count} stale notification(s)", stale.Count);
        return stale.Count;
    }

    /// <summary>
    /// Builds one Notification row per requested channel for a single user, persists them,
    /// simulates EMAIL/SMS sends, and pushes IN_APP rows live over SignalR.
    /// </summary>
    private async Task<List<NotificationDto>> CreateForUserAsync(
        Guid userId, string message, string category, List<string>? channels, Guid? sentByUserId = null)
    {
        var effectiveChannels = (channels is { Count: > 0 })
            ? channels
            : new List<string> { "IN_APP" };

        var now = DateTime.UtcNow;
        var entities = new List<Notification>();
        foreach (var channel in effectiveChannels)
        {
            entities.Add(new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = userId,
                Message = message,
                Category = category,
                Channel = channel.ToUpper(),
                Status = "UNREAD",
                CreatedAt = now,
                SentByUserId = sentByUserId
            });
        }

        var created = await repo.AddRangeAsync(entities);

        var hasInApp = false;
        foreach (var dto in created)
        {
            switch (dto.Channel)
            {
                case "EMAIL":
                case "SMS":
                    logger.LogInformation(
                        "Simulated {Channel} send to user {UserId} (category {Category}): {Message}",
                        dto.Channel, dto.UserId, dto.Category, dto.Message);
                    break;
                case "IN_APP":
                    hasInApp = true;
                    await hub.Clients.Group(userId.ToString()).ReceiveNotification(dto);
                    break;
            }
        }

        if (hasInApp)
            await PushUnreadCountAsync(userId);

        return created;
    }

    private async Task PushUnreadCountAsync(Guid userId)
    {
        var count = await repo.GetUnreadCountAsync(userId);
        await hub.Clients.Group(userId.ToString()).ReceiveUnreadCount(count);
    }
}
