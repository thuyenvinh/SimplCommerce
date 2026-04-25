#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Inventory.Models;

namespace SimplCommerce.Module.Inventory.Endpoints;

public static class InventoryAdminEndpoints
{
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

        return app;
    }
}
