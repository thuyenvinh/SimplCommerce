using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SimplCommerce.Module.Cms.Services;
using SimplCommerce.Module.Core.Models;
using SimplCommerce.Module.Core.Services;
using SimplCommerce.Module.Orders.Models;

namespace SimplCommerce.Module.Orders.Services
{
    // W3-G09: replaces the dead Razor-template path. The ApiService is a minimal-API
    // host with no MVC / RazorViewEngine wiring, and IRazorViewRenderer was never
    // registered in DI — so the previous SendEmailToUser would either no-op (when
    // the handler was unregistered, as it was for orders) or crash construction
    // (as it does for back-in-stock). Inline a small HTML builder instead. Real
    // template editing is admin-managed copy and would belong in a CMS string,
    // not a Razor file.
    public class OrderEmailService : IOrderEmailService
    {
        private readonly IEmailSender _emailSender;
        private readonly IEmailTemplateService _templates;

        public OrderEmailService(IEmailSender emailSender, IEmailTemplateService templates)
        {
            _emailSender = emailSender;
            _templates = templates;
        }

        public async Task SendEmailToUser(User user, Order order)
        {
            if (user is null || string.IsNullOrWhiteSpace(user.Email))
            {
                return;
            }

            // Wave 17: prefer the admin-editable CMS template. When no row is
            // active (cold start, admin deleted it, key not seeded yet) we fall
            // back to the inline default so email delivery never stops.
            var rendered = await _templates.RenderAsync("order-confirmation",
                BuildPlaceholders(order, user));
            string subject;
            string body;
            if (rendered is not null)
            {
                subject = rendered.Subject;
                body = rendered.BodyHtml;
            }
            else
            {
                subject = $"Order confirmation #{order.Id}";
                body = BuildOrderConfirmationHtml(order, user);
            }
            await _emailSender.SendEmailAsync(user.Email, subject, body, isHtml: true);
        }

        private static IReadOnlyDictionary<string, string> BuildPlaceholders(Order order, User user)
        {
            var ci = CultureInfo.InvariantCulture;
            return new Dictionary<string, string>
            {
                ["OrderId"] = order.Id.ToString(ci),
                ["CustomerName"] = WebUtility.HtmlEncode(user.FullName ?? user.Email ?? string.Empty),
                ["OrderTotal"] = order.OrderTotal.ToString("0.00", ci),
                ["SubTotal"] = order.SubTotal.ToString("0.00", ci),
                ["TaxAmount"] = order.TaxAmount.ToString("0.00", ci),
                ["ShippingAmount"] = order.ShippingFeeAmount.ToString("0.00", ci),
                ["DiscountAmount"] = order.DiscountAmount.ToString("0.00", ci),
            };
        }

        // Public so tests can pin invariants (HTML encoding of customer name etc.)
        // without going through the IEmailSender mock just to capture the body.
        public static string BuildOrderConfirmationHtml(Order order, User user)
        {
            // Money formatted invariant-culture so the email body is consistent
            // regardless of the server's current culture (which was an actual prod
            // bug fixed earlier in the migration — see CurrencyService null-culture).
            var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.Append("<!doctype html><html><body style=\"font-family:Arial,sans-serif;\">");
            sb.Append($"<h2>Thank you for your order #{order.Id}</h2>");
            sb.Append($"<p>Hi {WebUtility.HtmlEncode(user.FullName ?? user.Email)}, ");
            sb.Append("we've received your order and started processing it.</p>");
            sb.Append("<table cellpadding=\"6\" cellspacing=\"0\" border=\"0\" style=\"border-collapse:collapse;width:100%;max-width:600px;\">");
            sb.Append("<thead><tr style=\"border-bottom:1px solid #ddd;text-align:left;\">");
            sb.Append("<th>Product</th><th>Qty</th><th style=\"text-align:right;\">Price</th><th style=\"text-align:right;\">Subtotal</th></tr></thead><tbody>");
            foreach (var item in order.OrderItems ?? System.Array.Empty<OrderItem>())
            {
                var productName = WebUtility.HtmlEncode(item.Product?.Name ?? string.Empty);
                sb.Append("<tr style=\"border-bottom:1px solid #f0f0f0;\">");
                sb.Append($"<td>{productName}</td>");
                sb.Append($"<td>{item.Quantity}</td>");
                sb.Append($"<td style=\"text-align:right;\">{item.ProductPrice.ToString("0.00", ci)}</td>");
                sb.Append($"<td style=\"text-align:right;\">{(item.ProductPrice * item.Quantity).ToString("0.00", ci)}</td>");
                sb.Append("</tr>");
            }
            sb.Append("</tbody></table>");
            sb.Append("<p style=\"margin-top:16px;\">");
            sb.Append($"Subtotal: {order.SubTotal.ToString("0.00", ci)}<br/>");
            if (order.DiscountAmount > 0)
            {
                sb.Append($"Discount: -{order.DiscountAmount.ToString("0.00", ci)}<br/>");
            }
            sb.Append($"Tax: {order.TaxAmount.ToString("0.00", ci)}<br/>");
            sb.Append($"Shipping: {order.ShippingFeeAmount.ToString("0.00", ci)}<br/>");
            sb.Append($"<strong>Total: {order.OrderTotal.ToString("0.00", ci)}</strong>");
            sb.Append("</p>");
            sb.Append("<p>We'll send another email once your order ships.</p>");
            sb.Append("</body></html>");
            return sb.ToString();
        }
    }
}
