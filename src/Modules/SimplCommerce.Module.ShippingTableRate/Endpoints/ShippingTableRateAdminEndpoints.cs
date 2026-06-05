#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.ShippingTableRate.Models;

namespace SimplCommerce.Module.ShippingTableRate.Endpoints;

/// <summary>
/// Admin CRUD for table-rate shipping. The model is one row per
/// (country, optional state, optional ZIP, min cart subtotal) → shipping price.
/// Matching happens at checkout in <c>TableRateShippingServiceProvider</c>; this
/// endpoint just owns the storage side.
/// </summary>
public static class ShippingTableRateAdminEndpoints
{
    public record TableRateInput(string CountryId, long? StateOrProvinceId, long? DistrictId,
        string? ZipCode, string? Note, decimal MinOrderSubtotal, decimal ShippingPrice);
    public record TableRateItem(long Id, string CountryId, long? StateOrProvinceId, long? DistrictId,
        string? ZipCode, string? Note, decimal MinOrderSubtotal, decimal ShippingPrice);

    public static IEndpointRouteBuilder MapShippingTableRateAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/shipping/table-rates")
            .WithTags("Admin.Shipping.TableRates")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", async (IRepository<PriceAndDestination> repo) =>
        {
            var list = await repo.Query()
                .OrderBy(r => r.CountryId).ThenBy(r => r.MinOrderSubtotal)
                .Select(r => new TableRateItem(r.Id, r.CountryId, r.StateOrProvinceId, r.DistrictId,
                    r.ZipCode, r.Note, r.MinOrderSubtotal, r.ShippingPrice))
                .ToListAsync();
            return Results.Ok(list);
        });

        group.MapPost("/", async (TableRateInput input, IRepository<PriceAndDestination> repo) =>
        {
            if (string.IsNullOrWhiteSpace(input.CountryId))
            {
                return Results.BadRequest(new { error = "CountryId is required." });
            }
            var row = new PriceAndDestination
            {
                CountryId = input.CountryId,
                StateOrProvinceId = input.StateOrProvinceId,
                DistrictId = input.DistrictId,
                ZipCode = input.ZipCode ?? string.Empty,
                Note = input.Note ?? string.Empty,
                MinOrderSubtotal = input.MinOrderSubtotal,
                ShippingPrice = input.ShippingPrice,
            };
            repo.Add(row);
            await repo.SaveChangesAsync();
            return Results.Created($"/api/admin/shipping/table-rates/{row.Id}", new { row.Id });
        });

        group.MapPut("/{id:long}", async (long id, TableRateInput input, IRepository<PriceAndDestination> repo) =>
        {
            var row = await repo.Query().FirstOrDefaultAsync(r => r.Id == id);
            if (row is null) return Results.NotFound();
            row.CountryId = input.CountryId;
            row.StateOrProvinceId = input.StateOrProvinceId;
            row.DistrictId = input.DistrictId;
            row.ZipCode = input.ZipCode ?? string.Empty;
            row.Note = input.Note ?? string.Empty;
            row.MinOrderSubtotal = input.MinOrderSubtotal;
            row.ShippingPrice = input.ShippingPrice;
            await repo.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapDelete("/{id:long}", async (long id, IRepository<PriceAndDestination> repo) =>
        {
            var row = await repo.Query().FirstOrDefaultAsync(r => r.Id == id);
            if (row is null) return Results.NotFound();
            repo.Remove(row);
            await repo.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }
}
