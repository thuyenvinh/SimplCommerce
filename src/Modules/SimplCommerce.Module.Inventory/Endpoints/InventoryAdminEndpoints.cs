#nullable enable
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
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

        group.MapGet("/warehouses", async (IRepository<Warehouse> repo) =>
        {
            var list = await repo.Query()
                .Select(w => new { w.Id, w.Name, w.VendorId })
                .ToListAsync();
            return Results.Ok(list);
        });

        group.MapGet("/stocks", async (IRepository<Stock> repo, long? warehouseId = null) =>
        {
            var query = repo.Query().AsQueryable();
            if (warehouseId.HasValue) query = query.Where(s => s.WarehouseId == warehouseId);
            var list = await query.Select(s => new { s.Id, s.ProductId, s.WarehouseId, s.Quantity }).ToListAsync();
            return Results.Ok(list);
        });

        group.MapGet("/stock-history", async (IRepository<StockHistory> repo, long? productId = null, long? warehouseId = null, int page = 1, int pageSize = 50) =>
        {
            page = System.Math.Max(1, page);
            pageSize = System.Math.Clamp(pageSize, 1, 200);
            var query = repo.Query();
            if (productId.HasValue) query = query.Where(h => h.ProductId == productId);
            if (warehouseId.HasValue) query = query.Where(h => h.WarehouseId == warehouseId);
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
            ClaimsPrincipal principal) =>
        {
            if (input.AdjustedQuantity == 0)
            {
                return Results.BadRequest(new { error = "AdjustedQuantity must be non-zero." });
            }
            var productExists = await products.Query().AnyAsync(p => p.Id == input.ProductId && !p.IsDeleted);
            if (!productExists) return Results.BadRequest(new { error = "Product not found." });

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
            stock.Quantity += (int)input.AdjustedQuantity;

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
