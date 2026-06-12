using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Shared.DTOs;

namespace NotificationService.Hubs;

public interface INotificationClient
{
    /// <summary>Pushed to a user's group when a new notification is created for them.</summary>
    Task ReceiveNotification(NotificationDto notification);

    /// <summary>Pushed to a user's group whenever their unread count changes.</summary>
    Task ReceiveUnreadCount(int count);
}

[Authorize]
public class NotificationHub : Hub<INotificationClient>
{
    /// <summary>
    /// On connect, place the connection into a SignalR group named after the
    /// caller's user id so the server can push notifications to Clients.Group(userId).
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? Context.User?.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>No-op keep-alive the client can call to verify connectivity.</summary>
    public Task Ping() => Task.CompletedTask;
}
