#nullable enable
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Orders.Models;
using SimplCommerce.Module.Shipments.Models;

namespace SimplCommerce.Module.Shipments.Endpoints;

/// <summary>
/// Admin shipments API. The Shipment entity is a thin record (no status enum yet
/// in the domain), so this is intentionally a small surface: list per order +
/// create with tracking number + delete a mistaken shipment. Marking an order
/// "Shipped" / "Refunded" is the OrderStatus PATCH on /api/admin/orders/{id}/status —
/// that's where the customer-visible state machine lives.
/// </summary>
public static class ShipmentsAdminEndpoints
{
    public record ShipmentItemInput(long OrderItemId, long ProductId, int Quantity);
    public record ShipmentInput(long OrderId, long WarehouseId, string? TrackingNumber,
        System.Collections.Generic.IReadOnlyList<ShipmentItemInput> Items);

    public record AdminShipmentItem(long Id, long OrderId, string? TrackingNumber,
        long WarehouseId, System.DateTimeOffset CreatedOn, int ItemCount);

    public record AdminShipmentDetail(long Id, long OrderId, string? TrackingNumber,
        long WarehouseId, System.DateTimeOffset CreatedOn,
        System.Collections.Generic.IReadOnlyList<AdminShipmentLine> Items);
    public record AdminShipmentLine(long Id, long OrderItemId, long ProductId, string ProductName, int Quantity);

    public static IEndpointRouteBuilder MapShipmentsAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/shipments")
            .WithTags("Admin.Shipments")
            .RequireAuthorization("AdminOrVendor");

        group.MapGet("/", async (IRepository<Shipment> repo, long? orderId = null) =>
        {
            var query = repo.Query();
            if (orderId.HasValue) query = query.Where(s => s.OrderId == orderId);
            var list = await query.OrderByDescending(s => s.CreatedOn)
                .Select(s => new AdminShipmentItem(s.Id, s.OrderId, s.TrackingNumber,
                    s.WarehouseId, s.CreatedOn, s.Items.Count))
                .ToListAsync();
            return Results.Ok(list);
        });

        group.MapGet("/{id:long}", async (long id, IRepository<Shipment> repo) =>
        {
            var s = await repo.Query()
                .Include(x => x.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (s is null) return Results.NotFound();
            return Results.Ok(new AdminShipmentDetail(s.Id, s.OrderId, s.TrackingNumber,
                s.WarehouseId, s.CreatedOn,
                s.Items.Select(i => new AdminShipmentLine(i.Id, i.OrderItemId, i.ProductId,
                    i.Product?.Name ?? string.Empty, i.Quantity)).ToList()));
        });

        group.MapPost("/", async (
            ShipmentInput input,
            IRepository<Shipment> repo,
            IRepository<Order> orderRepo,
            ClaimsPrincipal principal) =>
        {
            if (!await orderRepo.Query().AnyAsync(o => o.Id == input.OrderId))
            {
                return Results.BadRequest(new { error = "OrderId does not resolve to an order." });
            }
            if (input.Items is null || input.Items.Count == 0)
            {
                return Results.BadRequest(new { error = "At least one item is required." });
            }

            var userIdRaw = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
            long.TryParse(userIdRaw, out var userId);

            var shipment = new Shipment
            {
                OrderId = input.OrderId,
                WarehouseId = input.WarehouseId,
                TrackingNumber = input.TrackingNumber ?? string.Empty,
                CreatedById = userId,
            };
            foreach (var line in input.Items)
            {
                shipment.Items.Add(new ShipmentItem
                {
                    OrderItemId = line.OrderItemId,
                    ProductId = line.ProductId,
                    Quantity = line.Quantity,
                });
            }
            repo.Add(shipment);
            await repo.SaveChangesAsync();
            return Results.Created($"/api/admin/shipments/{shipment.Id}", new { shipment.Id });
        });

        group.MapDelete("/{id:long}", async (long id, IRepository<Shipment> repo) =>
        {
            var s = await repo.Query().FirstOrDefaultAsync(x => x.Id == id);
            if (s is null) return Results.NotFound();
            repo.Remove(s);
            await repo.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }
}
