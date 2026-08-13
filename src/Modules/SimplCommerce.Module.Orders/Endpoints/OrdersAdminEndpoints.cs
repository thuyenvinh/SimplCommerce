#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Infrastructure.Web;
using SimplCommerce.Module.Orders.Models;

namespace SimplCommerce.Module.Orders.Endpoints;

public static class OrdersAdminEndpoints
{
    public record UpdateStatusRequest(OrderStatus NewStatus);

    public record SalesReportRow(System.DateTime Day, int OrderCount, decimal Revenue);
    public record SalesReportTotals(int OrderCount, decimal Revenue, System.DateTimeOffset From, System.DateTimeOffset To);

    public record AdminOrderItem(long ProductId, string ProductName, int Quantity, decimal ProductPrice, decimal DiscountAmount);
    public record AdminOrderAddress(string ContactName, string Phone, string AddressLine1, string? AddressLine2, string? City, string? ZipCode);
    public record AdminOrderDetail(
        long Id, DateTimeOffset CreatedOn, DateTimeOffset LatestUpdatedOn,
        OrderStatus OrderStatus, string? PaymentMethod, decimal SubTotal, decimal DiscountAmount,
        decimal TaxAmount, decimal ShippingAmount, decimal OrderTotal,
        long? CustomerId, string? CustomerEmail, string? CustomerFullName,
        AdminOrderAddress? ShippingAddress, AdminOrderAddress? BillingAddress,
        System.Collections.Generic.IReadOnlyList<AdminOrderItem> Items);

    public static IEndpointRouteBuilder MapOrdersAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/orders")
            .WithTags("Admin.Orders")
            .RequireAuthorization("AdminOrVendor");

        group.MapGet("/", async (
            IRepository<Order> repo,
            IVendorScope scope,
            OrderStatus? status = null,
            string? customerSearch = null,
            int page = 1,
            int pageSize = 20) =>
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var query = repo.Query().Include(o => o.Customer).AsQueryable();
            // Wave 6: vendors see only orders that contain their products. The
            // OrderService.CreateOrder pipeline writes Order.VendorId on every
            // sub-order (master order has VendorId=null), so filtering on the
            // sub-order rows surfaces exactly the work each vendor is responsible
            // for.
            if (scope.CurrentVendorId is { } vid) query = query.Where(o => o.VendorId == vid);
            if (status.HasValue) query = query.Where(o => o.OrderStatus == status);
            if (!string.IsNullOrWhiteSpace(customerSearch))
            {
                var pattern = $"%{customerSearch.Trim()}%";
                query = query.Where(o => EF.Functions.Like(o.Customer.FullName ?? string.Empty, pattern)
                    || EF.Functions.Like(o.Customer.Email ?? string.Empty, pattern));
            }
            var total = await query.CountAsync();
            var rows = await query.OrderByDescending(o => o.CreatedOn)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(o => new
                {
                    o.Id, o.CreatedOn, o.OrderTotal, o.OrderStatus,
                    Customer = new { o.Customer.Id, o.Customer.FullName, o.Customer.Email }
                })
                .ToListAsync();
            return Results.Ok(new { total, page, pageSize, items = rows });
        });

        group.MapGet("/{id:long}", async (long id, IRepository<Order> repo, IVendorScope scope) =>
        {
            var order = await repo.Query()
                .Include(o => o.Customer)
                .Include(o => o.ShippingAddress)
                .Include(o => o.BillingAddress)
                .Include(o => o.OrderItems).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order is null) return Results.NotFound();
            if (scope.CurrentVendorId is { } vid && order.VendorId != vid) return Results.NotFound();

            AdminOrderAddress? Map(Module.Orders.Models.OrderAddress? a) => a is null ? null
                : new AdminOrderAddress(a.ContactName, a.Phone, a.AddressLine1, a.AddressLine2, a.City, a.ZipCode);

            var dto = new AdminOrderDetail(
                order.Id, order.CreatedOn, order.LatestUpdatedOn,
                order.OrderStatus, order.PaymentMethod,
                order.SubTotal, order.DiscountAmount, order.TaxAmount,
                order.ShippingFeeAmount, order.OrderTotal,
                order.CustomerId, order.Customer?.Email, order.Customer?.FullName,
                Map(order.ShippingAddress), Map(order.BillingAddress),
                order.OrderItems.Select(i => new AdminOrderItem(
                    i.ProductId, i.Product?.Name ?? string.Empty,
                    i.Quantity, i.ProductPrice, i.DiscountAmount)).ToList());

            return Results.Ok(dto);
        });

        group.MapPatch("/{id:long}/status", async (long id, UpdateStatusRequest req,
            IRepository<Order> repo,
            IVendorScope scope,
            MediatR.IMediator mediator,
            System.Security.Claims.ClaimsPrincipal principal) =>
        {
            var order = await repo.Query().FirstOrDefaultAsync(o => o.Id == id);
            if (order is null) return Results.NotFound();
            if (scope.CurrentVendorId is { } vid && order.VendorId != vid) return Results.NotFound();

            var oldStatus = order.OrderStatus;
            if (oldStatus == req.NewStatus)
            {
                return Results.NoContent();
            }

            order.OrderStatus = req.NewStatus;
            order.LatestUpdatedOn = DateTimeOffset.UtcNow;
            await repo.SaveChangesAsync();

            // G05: publish OrderChanged so downstream handlers (OrderHistory writer,
            // email, SignalR) fire just like the cancel-by-background-service path —
            // not silent like the legacy admin patch.
            // FindFirstValue lives in the AspNetCore framework reference, which this
            // module deliberately doesn't pull. ClaimsPrincipal.FindFirst from corelib
            // is enough here.
            var rawUserId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? principal.FindFirst("sub")?.Value;
            long.TryParse(rawUserId, out var userId);
            await mediator.Publish(new Events.OrderChanged
            {
                OrderId = order.Id,
                Order = order,
                OldStatus = oldStatus,
                NewStatus = req.NewStatus,
                UserId = userId,
                Note = "admin status change",
            });

            return Results.NoContent();
        }).RequireAuthorization("AdminOnly");

        // Sales report: daily aggregates between [from, to). Both dates default to
        // last 30 days. Returns one row per yyyy-MM-dd with order count + gross revenue.
        // Counts orders in PaymentReceived / Invoiced / Shipping / Shipped / Complete —
        // i.e. anything past the "pending" line — to avoid pollution from abandoned carts.
        group.MapGet("/sales-report", async (IRepository<Order> repo, IVendorScope scope, System.DateTimeOffset? from = null, System.DateTimeOffset? to = null) =>
        {
            var until = to ?? DateTimeOffset.UtcNow;
            var since = from ?? until.AddDays(-30);
            var query = repo.Query()
                .Where(o => o.CreatedOn >= since && o.CreatedOn < until)
                .Where(o => o.OrderStatus == OrderStatus.PaymentReceived
                    || o.OrderStatus == OrderStatus.Invoiced
                    || o.OrderStatus == OrderStatus.Shipping
                    || o.OrderStatus == OrderStatus.Shipped
                    || o.OrderStatus == OrderStatus.Complete);
            // Wave 6: vendor sales report = vendor's sub-orders only. For admins,
            // exclude IsMasterOrder rows — they aggregate the same revenue that the
            // sub-orders below them already account for, so summing both would
            // double-count. Pure platform-owned orders (no vendor items) have
            // IsMasterOrder=false and are still included.
            if (scope.CurrentVendorId is { } vid)
                query = query.Where(o => o.VendorId == vid);
            else
                query = query.Where(o => !o.IsMasterOrder);
            var rows = await query
                .GroupBy(o => o.CreatedOn.Date)
                .Select(g => new SalesReportRow(
                    g.Key, g.Count(), g.Sum(o => o.OrderTotal)))
                .OrderBy(r => r.Day)
                .ToListAsync();
            var totals = new SalesReportTotals(
                rows.Sum(r => r.OrderCount),
                rows.Sum(r => r.Revenue),
                since, until);
            return Results.Ok(new { totals, rows });
        });

        return app;
    }
}
