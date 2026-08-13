using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Catalog.Models;
using SimplCommerce.Module.Core.Services;
using SimplCommerce.Module.Inventory.Models;

namespace SimplCommerce.Module.Inventory.Services
{
    // W3-G09: see OrderEmailService — Razor view rendering replaced with an inline
    // HTML builder so the service has no dependency on IRazorViewRenderer (which is
    // not registered in the minimal-API host's DI graph).
    public class StockSubscriptionService : IStockSubscriptionService
    {
        private readonly IRepository<ProductBackInStockSubscription> _productBackInStockSubscriptionRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IEmailSender _emailSender;

        public StockSubscriptionService(
            IRepository<ProductBackInStockSubscription> backInStockSubscriptionRepository,
            IEmailSender emailSender,
            IRepository<Product> productRepository)
        {
            _productBackInStockSubscriptionRepository = backInStockSubscriptionRepository;
            _emailSender = emailSender;
            _productRepository = productRepository;
        }

        public async Task ProductBackInStockSendNotificationsAsync(long productId)
        {
            var subscriptions = await _productBackInStockSubscriptionRepository
                .Query()
                .Where(o => o.ProductId == productId)
                .ToListAsync();

            if (subscriptions.Count == 0)
            {
                return;
            }

            var product = await _productRepository
                .Query()
                .Where(o => o.Id == productId)
                .FirstOrDefaultAsync();

            if (product is null)
            {
                return;
            }

            var subject = "Back in stock";
            var body = BuildBackInStockHtml(product);

            foreach (var subscription in subscriptions)
            {
                await _emailSender.SendEmailAsync(subscription.CustomerEmail, subject, body, isHtml: true);
                _productBackInStockSubscriptionRepository.Remove(subscription);
            }

            await _productBackInStockSubscriptionRepository.SaveChangesAsync();
        }

        public async Task ProductBackInStockSubscribeAsync(long productId, string customerEmail)
        {
            var subscription = new ProductBackInStockSubscription
            {
                ProductId = productId,
                CustomerEmail = customerEmail
            };

            _productBackInStockSubscriptionRepository.Add(subscription);

            await _productBackInStockSubscriptionRepository.SaveChangesAsync();
        }

        internal static string BuildBackInStockHtml(Product product)
        {
            var name = WebUtility.HtmlEncode(product.Name ?? string.Empty);
            var sb = new StringBuilder();
            sb.Append("<!doctype html><html><body style=\"font-family:Arial,sans-serif;\">");
            sb.Append($"<h2>{name} is back in stock</h2>");
            sb.Append($"<p>Good news — <strong>{name}</strong> is available again. ");
            sb.Append("Order soon before it sells out.</p>");
            sb.Append("</body></html>");
            return sb.ToString();
        }
    }
}
