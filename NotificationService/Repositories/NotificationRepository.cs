using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Data.Entities;
using NotificationService.Interfaces;
using Shared.DTOs;

namespace NotificationService.Repositories;

public class NotificationRepository(ServicesDbContext db) : INotificationRepository
{
    // â”€â”€ Queries â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public async Task<IEnumerable<NotificationDto>> GetForUserAsync(Guid userId, string? status, string? category)
    {
        var query = db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && n.Channel == "IN_APP");

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(n => n.Status == status);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(n => n.Category == category);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => ToDto(n))
            .ToListAsync();
    }

    public async Task<NotificationDto?> GetByIdAsync(Guid id)
    {
        var n = await db.Notifications
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.NotificationId == id);
        return n is null ? null : ToDto(n);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId) =>
        await db.Notifications
            .CountAsync(n => n.UserId == userId && n.Channel == "IN_APP" && n.Status == "UNREAD");

    

    public async Task<List<NotificationDto>> AddRangeAsync(IEnumerable<Notification> notifications)
    {
        var rows = notifications.ToList();
        db.Notifications.AddRange(rows);
        await db.SaveChangesAsync();
        return rows.Select(ToDto).ToList();
    }

    public async Task<bool> MarkReadAsync(Guid id, Guid userId)
    {
        var n = await db.Notifications
            .FirstOrDefaultAsync(x => x.NotificationId == id && x.UserId == userId);
        if (n is null) return false;

        n.Status = "READ";
        n.ReadAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<int> MarkAllReadAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        var rows = await db.Notifications
            .Where(n => n.UserId == userId && n.Channel == "IN_APP" && n.Status == "UNREAD")
            .ToListAsync();

        foreach (var n in rows)
        {
            n.Status = "READ";
            n.ReadAt = now;
        }

        if (rows.Count > 0)
            await db.SaveChangesAsync();

        return rows.Count;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId)
    {
        var n = await db.Notifications
            .FirstOrDefaultAsync(x => x.NotificationId == id && x.UserId == userId);
        if (n is null) return false;

        db.Notifications.Remove(n);
        await db.SaveChangesAsync();
        return true;
    }

    // â”€â”€ History / summary â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public async Task<NotificationHistoryDto> GetHistoryAsync(Guid userId, DateTime from, DateTime to)
    {
        var rows = await db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId
                && n.Channel == "IN_APP"
                && n.CreatedAt >= from
                && n.CreatedAt <= to)
            .ToListAsync();

        var byCategory = rows
            .GroupBy(n => n.Category)
            .Select(g => new CategorySummary(g.Key, g.Count()))
            .OrderByDescending(c => c.Count)
            .ToList();

        return new NotificationHistoryDto(
            UserId: userId,
            From: from,
            To: to,
            TotalCount: rows.Count,
            ReadCount: rows.Count(n => n.Status == "READ"),
            UnreadCount: rows.Count(n => n.Status == "UNREAD"),
            EscalatedCount: rows.Count(n => n.Status == "ESCALATED"),
            ByCategory: byCategory
        );
    }

    // â”€â”€ Escalation â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public async Task<List<Notification>> GetEscalatableAsync(IReadOnlyCollection<string> categories, DateTime olderThanUtc)
    {
        var cats = categories.ToList();
        return await db.Notifications
            .Where(n => n.Channel == "IN_APP"
                && n.Status == "UNREAD"
                && cats.Contains(n.Category)
                && n.CreatedAt < olderThanUtc)
            .ToListAsync();
    }

    public async Task MarkEscalatedAsync(IReadOnlyCollection<Guid> ids)
    {
        var idList = ids.ToList();
        if (idList.Count == 0) return;

        var rows = await db.Notifications
            .Where(n => idList.Contains(n.NotificationId))
            .ToListAsync();

        foreach (var n in rows)
            n.Status = "ESCALATED";

        if (rows.Count > 0)
            await db.SaveChangesAsync();
    }

    // â”€â”€ Recipient resolution â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public async Task<List<Guid>> ResolveRecipientsAsync(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return await db.Users
                .Where(u => u.IsActive)
                .Select(u => u.UserId)
                .ToListAsync();
        }

        return await db.Users
            .Where(u => u.IsActive && u.Role.RoleName == role)
            .Select(u => u.UserId)
            .ToListAsync();
    }

    // â”€â”€ Mapper â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public async Task<List<NotificationDto>> GetSentByUserAsync(Guid sentByUserId) =>
        await db.Notifications
            .AsNoTracking()
            .Where(n => n.SentByUserId == sentByUserId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto(
                n.NotificationId,
                n.UserId,
                n.Message,
                n.Category,
                n.Channel,
                n.Status,
                n.CreatedAt,
                n.ReadAt,
                n.SentByUserId,
                n.User.Name))
            .ToListAsync();

    private static NotificationDto ToDto(Notification n) =>
        new NotificationDto(
            NotificationId: n.NotificationId,
            UserId: n.UserId,
            Message: n.Message,
            Category: n.Category,
            Channel: n.Channel,
            Status: n.Status,
            CreatedAt: n.CreatedAt,
            ReadAt: n.ReadAt,
            SentByUserId: n.SentByUserId
        );
}
