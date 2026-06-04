#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Orders.Models;

namespace SimplCommerce.Module.Orders.Endpoints;

public static class OrdersAdminEndpoints
{
    public record UpdateStatusRequest(OrderStatus NewStatus);

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
            OrderStatus? status = null,
            string? customerSearch = null,
            int page = 1,
            int pageSize = 20) =>
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var query = repo.Query().Include(o => o.Customer).AsQueryable();
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

        group.MapGet("/{id:long}", async (long id, IRepository<Order> repo) =>
        {
            var order = await repo.Query()
                .Include(o => o.Customer)
                .Include(o => o.ShippingAddress)
                .Include(o => o.BillingAddress)
                .Include(o => o.OrderItems).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order is null) return Results.NotFound();

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

        group.MapPatch("/{id:long}/status", async (long id, UpdateStatusRequest req, IRepository<Order> repo) =>
        {
            var order = await repo.Query().FirstOrDefaultAsync(o => o.Id == id);
            if (order is null) return Results.NotFound();
            order.OrderStatus = req.NewStatus;
            order.LatestUpdatedOn = DateTimeOffset.UtcNow;
            repo.SaveChanges();
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly");

        return app;
    }
}
