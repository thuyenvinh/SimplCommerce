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
using SimplCommerce.Module.Catalog.Models;
using SimplCommerce.Module.Inventory.Models;

namespace SimplCommerce.Module.Inventory.Endpoints;

public static class InventoryAdminEndpoints
{
    public record StockAdjustmentInput(long ProductId, long WarehouseId, long AdjustedQuantity, string? Note);
    public record StockHistoryItem(long Id, long ProductId, string ProductName, long WarehouseId, string WarehouseName, long AdjustedQuantity, string? Note, System.DateTimeOffset CreatedOn);

    public static IEndpointRouteBuilder MapInventoryAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/inventory")
            .WithTags("Admin.Inventory")
            .RequireAuthorization("AdminOrVendor");

        // Wave 6: vendor inventory scope. Warehouses + stocks + stock history all
        // filter on Warehouse.VendorId so a vendor sees only their own depots.
        group.MapGet("/warehouses", async (IRepository<Warehouse> repo, IVendorScope scope) =>
        {
            var query = repo.Query();
            if (scope.CurrentVendorId is { } vid) query = query.Where(w => w.VendorId == vid);
            var list = await query
                .Select(w => new { w.Id, w.Name, w.VendorId })
                .ToListAsync();
            return Results.Ok(list);
        });

        group.MapGet("/stocks", async (IRepository<Stock> repo, IRepository<Warehouse> warehouses, IVendorScope scope, long? warehouseId = null) =>
        {
            var query = repo.Query().AsQueryable();
            if (warehouseId.HasValue) query = query.Where(s => s.WarehouseId == warehouseId);
            if (scope.CurrentVendorId is { } vid)
            {
                // Join through Warehouse since Stock has no VendorId of its own.
                query = query.Where(s => warehouses.Query().Any(w => w.Id == s.WarehouseId && w.VendorId == vid));
            }
            var list = await query.Select(s => new { s.Id, s.ProductId, s.WarehouseId, s.Quantity }).ToListAsync();
            return Results.Ok(list);
        });

        group.MapGet("/stock-history", async (IRepository<StockHistory> repo, IRepository<Warehouse> warehouses, IVendorScope scope, long? productId = null, long? warehouseId = null, int page = 1, int pageSize = 50) =>
        {
            page = System.Math.Max(1, page);
            pageSize = System.Math.Clamp(pageSize, 1, 200);
            var query = repo.Query();
            if (productId.HasValue) query = query.Where(h => h.ProductId == productId);
            if (warehouseId.HasValue) query = query.Where(h => h.WarehouseId == warehouseId);
            if (scope.CurrentVendorId is { } vid)
            {
                query = query.Where(h => warehouses.Query().Any(w => w.Id == h.WarehouseId && w.VendorId == vid));
            }
            var rows = await query.OrderByDescending(h => h.CreatedOn)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(h => new StockHistoryItem(
                    h.Id, h.ProductId, h.Product.Name, h.WarehouseId, h.Warehouse.Name,
                    h.AdjustedQuantity, h.Note, h.CreatedOn))
                .ToListAsync();
            return Results.Ok(rows);
        });

        group.MapPost("/stock-adjustments", async (
            StockAdjustmentInput input,
            IRepository<Stock> stocks,
            IRepository<StockHistory> history,
            IRepository<Product> products,
            IRepository<Warehouse> warehouses,
            IVendorScope scope,
            ClaimsPrincipal principal) =>
        {
            if (input.AdjustedQuantity == 0)
            {
                return Results.BadRequest(new { error = "AdjustedQuantity must be non-zero." });
            }
            var product = await products.Query().FirstOrDefaultAsync(p => p.Id == input.ProductId && !p.IsDeleted);
            if (product is null) return Results.BadRequest(new { error = "Product not found." });
            // Wave 6: vendor can only adjust stock on (a) their own warehouse and
            // (b) their own product. Either check failing returns 400 with a clear
            // reason, not a 404, so legitimate admin debugging gets actionable info.
            if (scope.CurrentVendorId is { } vid)
            {
                if (product.VendorId != vid)
                {
                    return Results.BadRequest(new { error = "Product is not owned by this vendor." });
                }
                var warehouseOwnedByVendor = await warehouses.Query()
                    .AnyAsync(w => w.Id == input.WarehouseId && w.VendorId == vid);
                if (!warehouseOwnedByVendor)
                {
                    return Results.BadRequest(new { error = "Warehouse is not owned by this vendor." });
                }
            }

            var stock = await stocks.Query()
                .FirstOrDefaultAsync(s => s.ProductId == input.ProductId && s.WarehouseId == input.WarehouseId);
            if (stock is null)
            {
                stock = new Stock
                {
                    ProductId = input.ProductId,
                    WarehouseId = input.WarehouseId,
                    Quantity = 0,
                };
                stocks.Add(stock);
            }
            // G13: stock-on-hand must never go negative. Reject the adjustment so
            // the caller can correct it rather than silently writing -X into Stock
            // (which then breaks reorder reports + cart availability checks).
            var newQty = stock.Quantity + (int)input.AdjustedQuantity;
            if (newQty < 0)
            {
                return Results.BadRequest(new { error = "Adjustment would drive stock negative.", currentQuantity = stock.Quantity });
            }
            stock.Quantity = newQty;

            var userIdRaw = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
            long.TryParse(userIdRaw, out var userId);
            var historyRow = new StockHistory
            {
                ProductId = input.ProductId,
                WarehouseId = input.WarehouseId,
                AdjustedQuantity = input.AdjustedQuantity,
                Note = input.Note,
                CreatedOn = System.DateTimeOffset.UtcNow,
                CreatedById = userId,
            };
            history.Add(historyRow);

            await stocks.SaveChangesAsync();
            return Results.Created($"/api/admin/inventory/stock-history?productId={input.ProductId}",
                new { historyRow.Id, stock.Quantity });
        });

        return app;
    }
}
