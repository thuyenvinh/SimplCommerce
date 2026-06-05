#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Orders.Models;

namespace SimplCommerce.Module.Orders.Endpoints;

/// <summary>
/// Storefront order history — the "my orders" page. Admin order CRUD lives in the
/// admin endpoint group.
/// </summary>
public static class OrdersStorefrontEndpoints
{
    public record OrderListItem(long Id, DateTimeOffset CreatedOn, decimal OrderTotal, OrderStatus OrderStatus);
    public record OrderItemDto(long ProductId, string ProductName, int Quantity, decimal ProductPrice);
    public record OrderDetailDto(long Id, DateTimeOffset CreatedOn, decimal SubTotal, decimal OrderTotal,
        OrderStatus OrderStatus, IReadOnlyList<OrderItemDto> Items);

    public static IEndpointRouteBuilder MapOrdersStorefrontEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/storefront/orders")
            .WithTags("Storefront.Orders")
            .RequireAuthorization("CustomerOnly");

        group.MapGet("/", async (IRepository<Order> repo, ClaimsPrincipal principal, int page = 1, int pageSize = 20) =>
        {
            if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var rows = await repo.Query()
                .Where(o => o.CustomerId == userId)
                .OrderByDescending(o => o.CreatedOn)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(o => new OrderListItem(o.Id, o.CreatedOn, o.OrderTotal, o.OrderStatus))
                .ToListAsync();
            return Results.Ok(rows);
        });

        group.MapGet("/{id:long}", async (long id, IRepository<Order> repo, ClaimsPrincipal principal) =>
        {
            if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
            var order = await repo.Query()
                .Include(o => o.OrderItems)
                .Where(o => o.Id == id && o.CustomerId == userId)
                .FirstOrDefaultAsync();
            if (order is null) return Results.NotFound();
            var detail = new OrderDetailDto(
                order.Id, order.CreatedOn, order.SubTotal, order.OrderTotal, order.OrderStatus,
                order.OrderItems.Select(i => new OrderItemDto(i.ProductId, i.Product?.Name ?? string.Empty, i.Quantity, i.ProductPrice)).ToList());
            return Results.Ok(detail);
        });

        return app;
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out long userId)
    {
        userId = 0;
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return long.TryParse(raw, out userId);
    }
}
