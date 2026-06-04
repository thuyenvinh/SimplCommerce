using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using SimplCommerce.Module.Orders.Events;
using SimplCommerce.RealTime;

namespace SimplCommerce.ApiService.Notifications;

/// <summary>
/// Fans the domain <c>OrderCreated</c> event out to every connected admin client
/// via the SignalR backplane. The handler runs in-process inside the ApiService,
/// but the Redis backplane means the message reaches connections held by any
/// Admin server instance (horizontal scale).
/// </summary>
public sealed class OrderCreatedAdminBroadcastHandler : INotificationHandler<OrderCreated>
{
    private readonly IHubContext<AdminNotificationHub, IAdminNotificationClient> _hub;

    public OrderCreatedAdminBroadcastHandler(IHubContext<AdminNotificationHub, IAdminNotificationClient> hub)
        => _hub = hub;

    public Task Handle(OrderCreated notification, CancellationToken cancellationToken)
    {
        var order = notification.Order;
        var payload = new AdminNotification(
            Kind: "order-created",
            Title: "New order",
            Message: $"Order #{order.Id} — {order.OrderTotal:C}",
            Link: $"/orders/{order.Id}",
            CreatedAt: DateTimeOffset.UtcNow);
        return _hub.Clients.Group("admins").Notification(payload);
    }
}
