#nullable enable
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Infrastructure.Web;
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
        long WarehouseId, ShipmentStatus Status, System.DateTimeOffset CreatedOn, int ItemCount);

    public record AdminShipmentDetail(long Id, long OrderId, string? TrackingNumber,
        long WarehouseId, ShipmentStatus Status, System.DateTimeOffset CreatedOn,
        System.Collections.Generic.IReadOnlyList<AdminShipmentLine> Items);
    public record AdminShipmentLine(long Id, long OrderItemId, long ProductId, string ProductName, int Quantity);

    // G01: shipment status transitions. Pending → Shipped → Delivered is the happy
    // path; Returned/Cancelled are terminal off-ramps. Rejected transitions return
    // 400 so admins can't accidentally regress a delivered parcel back to Pending.
    public record UpdateShipmentStatusRequest(ShipmentStatus NewStatus);

    public static IEndpointRouteBuilder MapShipmentsAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/shipments")
            .WithTags("Admin.Shipments")
            .RequireAuthorization("AdminOrVendor");

        group.MapGet("/", async (IRepository<Shipment> repo, IVendorScope scope, long? orderId = null) =>
        {
            var query = repo.Query();
            // Wave 6: vendor sees only shipments they created. Shipment.VendorId is
            // stamped at creation time below (when running as vendor) — historical
            // rows with null VendorId stay admin-only.
            if (scope.CurrentVendorId is { } vid) query = query.Where(s => s.VendorId == vid);
            if (orderId.HasValue) query = query.Where(s => s.OrderId == orderId);
            var list = await query.OrderByDescending(s => s.CreatedOn)
                .Select(s => new AdminShipmentItem(s.Id, s.OrderId, s.TrackingNumber,
                    s.WarehouseId, s.Status, s.CreatedOn, s.Items.Count))
                .ToListAsync();
            return Results.Ok(list);
        });

        group.MapGet("/{id:long}", async (long id, IRepository<Shipment> repo, IVendorScope scope) =>
        {
            var s = await repo.Query()
                .Include(x => x.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (s is null) return Results.NotFound();
            if (scope.CurrentVendorId is { } vid && s.VendorId != vid) return Results.NotFound();
            return Results.Ok(new AdminShipmentDetail(s.Id, s.OrderId, s.TrackingNumber,
                s.WarehouseId, s.Status, s.CreatedOn,
                s.Items.Select(i => new AdminShipmentLine(i.Id, i.OrderItemId, i.ProductId,
                    i.Product?.Name ?? string.Empty, i.Quantity)).ToList()));
        });

        group.MapPatch("/{id:long}/status", async (long id, UpdateShipmentStatusRequest req, IRepository<Shipment> repo, IVendorScope scope) =>
        {
            var s = await repo.Query().FirstOrDefaultAsync(x => x.Id == id);
            if (s is null) return Results.NotFound();
            if (scope.CurrentVendorId is { } vid && s.VendorId != vid) return Results.NotFound();
            if (!IsValidTransition(s.Status, req.NewStatus))
            {
                return Results.BadRequest(new { error = $"Invalid transition {s.Status} → {req.NewStatus}." });
            }
            s.Status = req.NewStatus;
            s.LatestUpdatedOn = System.DateTimeOffset.UtcNow;
            await repo.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapPost("/", async (
            ShipmentInput input,
            IRepository<Shipment> repo,
            IRepository<Order> orderRepo,
            IVendorScope scope,
            ClaimsPrincipal principal) =>
        {
            var order = await orderRepo.Query().FirstOrDefaultAsync(o => o.Id == input.OrderId);
            if (order is null)
            {
                return Results.BadRequest(new { error = "OrderId does not resolve to an order." });
            }
            // Wave 6: vendor can only ship an order that belongs to them.
            if (scope.CurrentVendorId is { } vid && order.VendorId != vid)
            {
                return Results.BadRequest(new { error = "Order is not owned by this vendor." });
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
                // Stamp VendorId so subsequent vendor queries can find this row.
                VendorId = scope.CurrentVendorId ?? order.VendorId,
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

        group.MapDelete("/{id:long}", async (long id, IRepository<Shipment> repo, IVendorScope scope) =>
        {
            var s = await repo.Query().FirstOrDefaultAsync(x => x.Id == id);
            if (s is null) return Results.NotFound();
            if (scope.CurrentVendorId is { } vid && s.VendorId != vid) return Results.NotFound();
            repo.Remove(s);
            await repo.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }

    private static bool IsValidTransition(ShipmentStatus from, ShipmentStatus to)
    {
        if (from == to) return true;
        return from switch
        {
            ShipmentStatus.Pending => to is ShipmentStatus.Shipped or ShipmentStatus.Cancelled,
            ShipmentStatus.Shipped => to is ShipmentStatus.Delivered or ShipmentStatus.Returned,
            ShipmentStatus.Delivered => to is ShipmentStatus.Returned,
            // Returned + Cancelled are terminal.
            _ => false,
        };
    }
}
