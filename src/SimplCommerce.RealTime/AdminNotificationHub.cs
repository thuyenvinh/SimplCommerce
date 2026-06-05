using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SimplCommerce.RealTime;

/// <summary>
/// Admin-only realtime channel. Lives in a small shared lib (no Module.Core
/// transitive dependencies) so both the ApiService publisher and the Admin host
/// can reference it without pulling the legacy Newtonsoft.Json /
/// Microsoft.AspNet.WebApi.Client surface into the Admin app — that pollutes
/// HttpClient.PostAsJsonAsync overloads.
///
/// The Redis backplane matches messages by <c>typeof(THub).FullName</c>, so the
/// shared lib type identity is what makes cross-host publish work.
/// </summary>
[Authorize(Roles = "admin")]
public class AdminNotificationHub : Hub<IAdminNotificationClient>
{
    public override Task OnConnectedAsync()
        => Groups.AddToGroupAsync(Context.ConnectionId, "admins");
}

public interface IAdminNotificationClient
{
    Task Notification(AdminNotification notification);
}

public record AdminNotification(
    string Kind,
    string Title,
    string Message,
    string? Link,
    DateTimeOffset CreatedAt);
