using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SimplCommerce.RealTime;

/// <summary>
/// Realtime channel for admin + vendor users. Lives in a small shared lib (no
/// Module.Core transitive dependencies) so both the ApiService publisher and
/// the Admin host can reference it without pulling the legacy Newtonsoft.Json /
/// Microsoft.AspNet.WebApi.Client surface into the Admin app — that pollutes
/// HttpClient.PostAsJsonAsync overloads.
///
/// The Redis backplane matches messages by <c>typeof(THub).FullName</c>, so the
/// shared lib type identity is what makes cross-host publish work.
///
/// Wave 13: relaxed from admin-only to any authenticated user; group membership
/// drives the audience instead. On connect the hub joins:
///   • "admins"          — when the principal has the "admin" role
///   • "vendor-{id}"     — when the JWT carries a vendor_id claim
/// Publishers pick the group(s) to target, so a sub-order notification can fan
/// out to <c>vendor-7</c> for the vendor + <c>admins</c> for platform oversight.
/// </summary>
[Authorize]
public class AdminNotificationHub : Hub<IAdminNotificationClient>
{
    public override async Task OnConnectedAsync()
    {
        var user = Context.User;
        if (user is null)
        {
            await base.OnConnectedAsync();
            return;
        }
        if (user.IsInRole("admin"))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
        }
        var vendorIdClaim = user.FindFirst("vendor_id")?.Value;
        if (long.TryParse(vendorIdClaim, out var vid))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"vendor-{vid}");
        }
        await base.OnConnectedAsync();
    }
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
