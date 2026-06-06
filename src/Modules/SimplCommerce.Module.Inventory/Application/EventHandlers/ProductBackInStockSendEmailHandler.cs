using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SimplCommerce.Module.Inventory.Services;

namespace SimplCommerce.Module.Inventory.Event
{
    public class ProductBackInStockSendEmailHandler : INotificationHandler<ProductBackInStock>
    {
        public readonly IStockSubscriptionService _stockSubscriptionService;
        private readonly ILogger<ProductBackInStockSendEmailHandler> _logger;

        public ProductBackInStockSendEmailHandler(
            IStockSubscriptionService stockSubscriptionService,
            ILogger<ProductBackInStockSendEmailHandler> logger)
        {
            _stockSubscriptionService = stockSubscriptionService;
            _logger = logger;
        }

        public async Task Handle(ProductBackInStock notification, CancellationToken cancellationToken)
        {
            // W3-G09: same rationale as AfterOrderCreatedSendEmailHanlder — the
            // ProductBackInStock notification is published from inside
            // StockService.UpdateStock, where a thrown exception would roll back the
            // whole stock adjustment. Swallow + log so a missing Razor template (or any
            // SMTP transient) doesn't poison stock movements.
            try
            {
                await _stockSubscriptionService.ProductBackInStockSendNotificationsAsync(notification.ProductId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send back-in-stock notification for product {ProductId}.", notification.ProductId);
            }
        }
    }
}
