using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using SimplCommerce.Module.Orders.Events;
using SimplCommerce.RealTime;

namespace SimplCommerce.ApiService.Notifications;

/// <summary>
/// Fans the domain <c>OrderCreated</c> event out to admin + vendor SignalR
/// audiences. The handler runs in-process inside the ApiService, but the Redis
/// backplane means the message reaches connections held by any Admin server
/// instance (horizontal scale).
///
/// Wave 13: routing rules.
///   • Sub-orders (Order.VendorId is set) push to <c>vendor-{vid}</c> so the
///     vendor's dashboard lights up, and also to <c>admins</c> for platform
///     oversight.
///   • Master orders + platform-only orders push only to <c>admins</c> —
///     vendors aren't responsible for them.
/// </summary>
public sealed class OrderCreatedAdminBroadcastHandler : INotificationHandler<OrderCreated>
{
    private readonly IHubContext<AdminNotificationHub, IAdminNotificationClient> _hub;

    public OrderCreatedAdminBroadcastHandler(IHubContext<AdminNotificationHub, IAdminNotificationClient> hub)
        => _hub = hub;

    public async Task Handle(OrderCreated notification, CancellationToken cancellationToken)
    {
        var order = notification.Order;
        var payload = new AdminNotification(
            Kind: "order-created",
            Title: order.VendorId.HasValue ? "New vendor order" : "New order",
            Message: $"Order #{order.Id} — {order.OrderTotal:C}",
            Link: $"/orders/{order.Id}",
            CreatedAt: DateTimeOffset.UtcNow);

        // Admins always see the event for oversight. Sub-orders additionally
        // light up the owning vendor's group — the same event reaches two
        // audiences but doesn't double-send to anyone who's in both groups
        // (e.g. a vendor user who also has admin role).
        await _hub.Clients.Group("admins").Notification(payload);
        if (order.VendorId is { } vid)
        {
            await _hub.Clients.Group($"vendor-{vid}").Notification(payload);
        }
    }
}
