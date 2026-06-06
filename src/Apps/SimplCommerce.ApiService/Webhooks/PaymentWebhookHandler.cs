#nullable enable
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Orders.Events;
using SimplCommerce.Module.Orders.Models;
using SimplCommerce.Module.Payments.Models;

namespace SimplCommerce.ApiService.Webhooks;

/// <summary>
/// G02: applies the domain side-effect for a verified webhook event. The webhook
/// signature gate (WebhookSignatureVerifier) keeps this code from running on
/// spoofed payloads; everything below assumes the payload is authentic.
///
/// Lookup strategy is <see cref="Payment.GatewayTransactionId"/>-based: the
/// provider's checkout flow (e.g. VnpayEndpoints.RecordPaymentAsync) writes the
/// upstream transaction id when the Payment row is first created. The handler
/// finds the Payment, mutates it + the linked Order, and publishes OrderChanged
/// so downstream history / email / SignalR fire just like a manual admin PATCH.
///
/// If no Payment matches (Stripe webhook arrives before checkout-side wiring
/// exists, or a manual test event from the Stripe dashboard) we log + return —
/// the provider still gets 202 and won't retry, which is the right behavior for
/// an event we genuinely can't act on.
/// </summary>
public interface IPaymentWebhookHandler
{
    Task HandleStripeAsync(string payload, CancellationToken ct);
}

public sealed class PaymentWebhookHandler : IPaymentWebhookHandler
{
    private readonly IRepository<Payment> _payments;
    private readonly IRepository<Order> _orders;
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentWebhookHandler> _logger;

    public PaymentWebhookHandler(
        IRepository<Payment> payments,
        IRepository<Order> orders,
        IMediator mediator,
        ILogger<PaymentWebhookHandler> logger)
    {
        _payments = payments;
        _orders = orders;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task HandleStripeAsync(string payload, CancellationToken ct)
    {
        StripeEvent? evt;
        try
        {
            evt = ParseStripe(payload);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Stripe webhook payload was verified but couldn't be parsed.");
            return;
        }
        if (evt is null)
        {
            return;
        }

        switch (evt.Type)
        {
            case "payment_intent.succeeded":
                await MarkSucceededAsync(evt.PaymentIntentId, ct);
                break;
            case "payment_intent.payment_failed":
                await MarkFailedAsync(evt.PaymentIntentId, ct);
                break;
            case "charge.refunded":
                await ApplyRefundAsync(evt.PaymentIntentId, evt.AmountRefunded, ct);
                break;
            default:
                _logger.LogDebug("Stripe event {Type} received with no handler — gate verified, ignored.", evt.Type);
                break;
        }
    }

    private async Task MarkSucceededAsync(string? paymentIntentId, CancellationToken ct)
    {
        var (payment, order) = await LoadAsync(paymentIntentId, ct);
        if (payment is null || order is null) return;

        if (payment.Status == PaymentStatus.Succeeded && order.OrderStatus >= OrderStatus.PaymentReceived)
        {
            // Already applied (Stripe retries succeed-events; we're idempotent).
            return;
        }

        var oldStatus = order.OrderStatus;
        payment.Status = PaymentStatus.Succeeded;
        payment.LatestUpdatedOn = System.DateTimeOffset.UtcNow;
        if (order.OrderStatus < OrderStatus.PaymentReceived)
        {
            order.OrderStatus = OrderStatus.PaymentReceived;
            order.LatestUpdatedOn = System.DateTimeOffset.UtcNow;
        }
        await _payments.SaveChangesAsync();

        await _mediator.Publish(new OrderChanged
        {
            OrderId = order.Id,
            Order = order,
            OldStatus = oldStatus,
            NewStatus = order.OrderStatus,
            Note = "stripe payment_intent.succeeded",
        }, ct);
    }

    private async Task MarkFailedAsync(string? paymentIntentId, CancellationToken ct)
    {
        var (payment, order) = await LoadAsync(paymentIntentId, ct);
        if (payment is null || order is null) return;

        // PaymentFailed is sticky: don't downgrade a Succeeded payment just because
        // a stale failed-event arrives out of order. Real money is on the line.
        if (payment.Status == PaymentStatus.Succeeded) return;

        var oldStatus = order.OrderStatus;
        payment.Status = PaymentStatus.Failed;
        payment.LatestUpdatedOn = System.DateTimeOffset.UtcNow;
        if (order.OrderStatus < OrderStatus.PaymentReceived)
        {
            order.OrderStatus = OrderStatus.PaymentFailed;
            order.LatestUpdatedOn = System.DateTimeOffset.UtcNow;
        }
        await _payments.SaveChangesAsync();

        await _mediator.Publish(new OrderChanged
        {
            OrderId = order.Id,
            Order = order,
            OldStatus = oldStatus,
            NewStatus = order.OrderStatus,
            Note = "stripe payment_intent.payment_failed",
        }, ct);
    }

    private async Task ApplyRefundAsync(string? paymentIntentId, decimal amountRefunded, CancellationToken ct)
    {
        if (amountRefunded <= 0) return;
        var (payment, order) = await LoadAsync(paymentIntentId, ct);
        if (payment is null || order is null) return;

        // Stripe sends amount_refunded as the CUMULATIVE refunded amount — not the
        // delta — so we set it directly rather than adding. This keeps duplicate
        // webhook deliveries idempotent.
        var previousRefunded = payment.RefundedAmount ?? 0m;
        if (amountRefunded <= previousRefunded) return;

        payment.RefundedAmount = amountRefunded;
        payment.RefundedOn = System.DateTimeOffset.UtcNow;
        payment.LatestUpdatedOn = System.DateTimeOffset.UtcNow;
        var isFullRefund = amountRefunded >= payment.Amount;
        if (isFullRefund) payment.Status = PaymentStatus.Refunded;

        var oldStatus = order.OrderStatus;
        if (isFullRefund && order.OrderStatus != OrderStatus.Refunded)
        {
            order.OrderStatus = OrderStatus.Refunded;
            order.LatestUpdatedOn = System.DateTimeOffset.UtcNow;
        }
        await _payments.SaveChangesAsync();

        await _mediator.Publish(new OrderChanged
        {
            OrderId = order.Id,
            Order = order,
            OldStatus = oldStatus,
            NewStatus = order.OrderStatus,
            Note = $"stripe charge.refunded ({amountRefunded:0.##})",
        }, ct);
    }

    private async Task<(Payment? payment, Order? order)> LoadAsync(string? paymentIntentId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(paymentIntentId))
        {
            _logger.LogDebug("Stripe webhook event had no payment_intent.id — nothing to correlate.");
            return (null, null);
        }
        var payment = await _payments.Query()
            .FirstOrDefaultAsync(p => p.GatewayTransactionId == paymentIntentId, ct);
        if (payment is null)
        {
            _logger.LogInformation("Stripe webhook event references unknown payment_intent {Pi} — most likely the checkout flow hasn't been wired to persist the GatewayTransactionId yet.", paymentIntentId);
            return (null, null);
        }
        var order = await _orders.Query().FirstOrDefaultAsync(o => o.Id == payment.OrderId, ct);
        return (payment, order);
    }

    // Public so the JSON-shape invariants can be pinned by unit tests without
    // needing an InternalsVisibleTo dance. The records / methods exposed here are
    // pure functions over the Stripe event payload.
    public record StripeEvent(string Type, string? PaymentIntentId, decimal AmountRefunded);

    public static StripeEvent? ParseStripe(string payload)
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;
        if (!root.TryGetProperty("type", out var typeEl) || typeEl.ValueKind != JsonValueKind.String)
        {
            return null;
        }
        var type = typeEl.GetString()!;
        if (!root.TryGetProperty("data", out var data) ||
            !data.TryGetProperty("object", out var obj))
        {
            return new StripeEvent(type, null, 0m);
        }

        string? paymentIntentId = type switch
        {
            // payment_intent.* events: data.object is the PaymentIntent itself
            var t when t.StartsWith("payment_intent.") => TryString(obj, "id"),
            // charge.* events: data.object is the Charge, payment_intent is on it
            var t when t.StartsWith("charge.") => TryString(obj, "payment_intent"),
            _ => null,
        };

        decimal amountRefunded = 0m;
        if (type == "charge.refunded" && obj.TryGetProperty("amount_refunded", out var amtEl)
            && amtEl.ValueKind == JsonValueKind.Number
            && amtEl.TryGetInt64(out var minor))
        {
            // Stripe amount is in the smallest currency unit (cents for USD,
            // VND has no fractional unit but Stripe still uses minor units).
            amountRefunded = minor / 100m;
        }

        return new StripeEvent(type, paymentIntentId, amountRefunded);
    }

    private static string? TryString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;
}
