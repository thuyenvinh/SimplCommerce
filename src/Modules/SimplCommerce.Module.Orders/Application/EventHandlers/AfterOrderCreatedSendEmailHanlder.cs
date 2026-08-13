using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SimplCommerce.Module.Orders.Services;

namespace SimplCommerce.Module.Orders.Events
{
    public class AfterOrderCreatedSendEmailHanlder : INotificationHandler<AfterOrderCreated>
    {
        private readonly IOrderEmailService _orderEmailService;
        private readonly ILogger<AfterOrderCreatedSendEmailHanlder> _logger;

        public AfterOrderCreatedSendEmailHanlder(
            IOrderEmailService orderEmailService,
            ILogger<AfterOrderCreatedSendEmailHanlder> logger)
        {
            _orderEmailService = orderEmailService;
            _logger = logger;
        }

        public async Task Handle(AfterOrderCreated notification, CancellationToken cancellationToken)
        {
            // W3-G09: email rendering is fire-and-forget — a missing Razor template or
            // a transient SMTP failure must NOT roll back the order. OrderService.CreateOrder
            // awaits AfterOrderCreated synchronously, so an unhandled exception here would
            // crash the API response after the order is already committed: the customer
            // sees a 500, retries with the same checkout, and (without G04 idempotency)
            // would create a duplicate. Swallow + log instead.
            try
            {
                await _orderEmailService.SendEmailToUser(notification.Order.Customer, notification.Order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send order confirmation email for order {OrderId}.", notification.Order.Id);
            }
        }
    }
}
