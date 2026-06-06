#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Orders.Events;
using SimplCommerce.Module.Orders.Models;
using SimplCommerce.Module.Payments.Models;

namespace SimplCommerce.Module.Payments.Endpoints;

public static class PaymentsAdminEndpoints
{
    public record PaymentProviderItem(string Id, string Name, bool IsEnabled);
    public record PaymentProviderDetail(string Id, string Name, bool IsEnabled, string? AdditionalSettings);
    public record PaymentProviderInput(bool IsEnabled, string? AdditionalSettings);

    // G03: refunds. Payload-driven so partial refunds are possible (Amount < Payment.Amount).
    // The endpoint validates: payment exists, amount > 0, and cumulative refunds don't
    // exceed the captured Amount. On success it writes Payment.RefundedAmount, flips
    // PaymentStatus.Refunded on a full refund, moves Order to Refunded, and publishes
    // OrderChanged so OrderHistory / email / SignalR handlers fire.
    public record RefundRequest(long OrderId, decimal Amount, string? Reason);

    public static IEndpointRouteBuilder MapPaymentsAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/payments")
            .WithTags("Admin.Payments")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/providers", async (IRepositoryWithTypedId<PaymentProvider, string> repo) =>
            Results.Ok(await repo.Query()
                .OrderBy(p => p.Name)
                .Select(p => new PaymentProviderItem(p.Id, p.Name, p.IsEnabled))
                .ToListAsync()));

        group.MapGet("/providers/{id}", async (string id, IRepositoryWithTypedId<PaymentProvider, string> repo) =>
        {
            var p = await repo.Query().FirstOrDefaultAsync(x => x.Id == id);
            return p is null
                ? Results.NotFound()
                : Results.Ok(new PaymentProviderDetail(p.Id, p.Name, p.IsEnabled, p.AdditionalSettings));
        });

        group.MapPut("/providers/{id}", async (string id, PaymentProviderInput input, IRepositoryWithTypedId<PaymentProvider, string> repo) =>
        {
            var p = await repo.Query().FirstOrDefaultAsync(x => x.Id == id);
            if (p is null) return Results.NotFound();
            p.IsEnabled = input.IsEnabled;
            p.AdditionalSettings = input.AdditionalSettings ?? string.Empty;
            await repo.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapGet("/", async (IRepository<Payment> repo, int page = 1, int pageSize = 20) =>
        {
            page = System.Math.Max(1, page);
            pageSize = System.Math.Clamp(pageSize, 1, 100);
            var total = await repo.Query().CountAsync();
            var rows = await repo.Query().OrderByDescending(p => p.CreatedOn)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(p => new { p.Id, p.OrderId, p.PaymentMethod, p.PaymentFee, p.Amount, p.RefundedAmount, p.Status, p.CreatedOn })
                .ToListAsync();
            return Results.Ok(new { total, page, pageSize, items = rows });
        });

        group.MapPost("/refunds", async (
            RefundRequest req,
            IRepository<Payment> payments,
            IRepository<Order> orders,
            IMediator mediator) =>
        {
            if (req.Amount <= 0)
            {
                return Results.BadRequest(new { error = "Refund amount must be positive." });
            }
            var payment = await payments.Query()
                .Where(p => p.OrderId == req.OrderId && p.Status == PaymentStatus.Succeeded)
                .OrderByDescending(p => p.CreatedOn)
                .FirstOrDefaultAsync();
            if (payment is null)
            {
                return Results.BadRequest(new { error = "No captured payment found for this order." });
            }
            var refundedSoFar = payment.RefundedAmount ?? 0m;
            if (refundedSoFar + req.Amount > payment.Amount)
            {
                return Results.BadRequest(new
                {
                    error = "Refund would exceed captured amount.",
                    captured = payment.Amount,
                    alreadyRefunded = refundedSoFar,
                    remaining = payment.Amount - refundedSoFar,
                });
            }
            var order = await orders.Query().FirstOrDefaultAsync(o => o.Id == req.OrderId);
            if (order is null) return Results.NotFound();

            payment.RefundedAmount = refundedSoFar + req.Amount;
            payment.RefundedOn = DateTimeOffset.UtcNow;
            payment.LatestUpdatedOn = DateTimeOffset.UtcNow;
            var isFullRefund = payment.RefundedAmount >= payment.Amount;
            if (isFullRefund)
            {
                payment.Status = PaymentStatus.Refunded;
            }

            var oldStatus = order.OrderStatus;
            if (isFullRefund)
            {
                order.OrderStatus = OrderStatus.Refunded;
                order.LatestUpdatedOn = DateTimeOffset.UtcNow;
            }
            await payments.SaveChangesAsync();

            // W3-G13: publish for partial refunds too. The Order status only flips on
            // a full refund, but OrderHistory / customer-email handlers still need to
            // know that a refund occurred (with the amount in the note) — otherwise the
            // customer gets money back with no audit trail or notification.
            var note = string.IsNullOrWhiteSpace(req.Reason)
                ? $"refund {req.Amount:0.##}"
                : $"refund {req.Amount:0.##}: {req.Reason}";
            await mediator.Publish(new OrderChanged
            {
                OrderId = order.Id,
                Order = order,
                OldStatus = oldStatus,
                NewStatus = order.OrderStatus,
                Note = note,
            });
            return Results.Ok(new
            {
                paymentId = payment.Id,
                refundedAmount = payment.RefundedAmount,
                remaining = payment.Amount - (payment.RefundedAmount ?? 0m),
                paymentStatus = payment.Status,
                orderStatus = order.OrderStatus,
            });
        });

        return app;
    }
}
